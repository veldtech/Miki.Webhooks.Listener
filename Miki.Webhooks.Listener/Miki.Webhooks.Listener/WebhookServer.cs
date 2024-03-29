﻿namespace Miki.Webhooks.Listener
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Miki.Configuration;
    using Miki.Logging;
    using Newtonsoft.Json;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
	using Microsoft.Extensions.DependencyInjection;

    public class WebhookServer
	{
		public class WebhookResponse
		{
			[JsonProperty("success")]
			public bool Success;

			[JsonProperty("error")]
			public string Error;

			public static WebhookResponse AsError(string errorMessage)
				=> new WebhookResponse { Error = errorMessage };
			public static string AsErrorJson(string errorMessage)
				=> JsonConvert.SerializeObject(AsError(errorMessage));

			public static WebhookResponse AsSuccess()
				=> new WebhookResponse { Success = true };
			public static string AsSuccessJson()
				=> JsonConvert.SerializeObject(AsSuccess());
		}

		private readonly Dictionary<string, IWebhookEvent> allWebhookEvents 
            = new Dictionary<string, IWebhookEvent>();
		private readonly ConfigurationManager configuration = new ConfigurationManager();

		private readonly string authKey;
        private readonly IServiceProvider services;

        public WebhookServer(string key, IServiceProvider services)
        {
            authKey = key;
            this.services = services;
        }

		public void AddWebhookRoute(IWebhookEvent ev)
		{
			foreach (var x in ev.AcceptedUrls)
			{
				allWebhookEvents.Add(x, ev);
			}
		}

		public async Task RunAsync()
		{
			if (File.Exists("./config.json"))
			{
				await configuration.ImportAsync(
					new JsonSerializationProvider(),
					"./config.json");
			}

			await configuration.ExportAsync(
				new JsonSerializationProvider(),
				"./config.json");

			var host = new WebHostBuilder()
				.UseKestrel()
				.UseUrls("http://0.0.0.0:5000")
				.Configure(
					app => app.Map("/submit", SubmissionHandler)
						.Map("/ping", PingHandler)
				)
				.Build();

			await host.RunAsync();
		}

		private void PingHandler(IApplicationBuilder app)
		{
			app.Run(async context =>
			{
				await context.Response.WriteAsync("pong!");
			});
		}

		private void SubmissionHandler(IApplicationBuilder app)
		{
			app.Run(async context =>
			{
				if(context.Request.Method != "POST")
				{
					return;
				}

                if (!context.Request.Query.TryGetValue("type", out var value))
				{
					await context.Response.WriteAsync(WebhookResponse.AsErrorJson("no webhook type defined."));
					return;
				}

				if (authKey != null)
				{
					if (!context.Request.Query.TryGetValue("key", out var auth))
					{
						await context.Response.WriteAsync(WebhookResponse.AsErrorJson("no authorization provided."));
						return;
					}

					if(auth != authKey)
					{
						await context.Response.WriteAsync(WebhookResponse.AsErrorJson("unauthorized."));
						return;
					}
				}

				var type = value.FirstOrDefault();
                if(!allWebhookEvents.TryGetValue(type, out var ev))
				{
					await context.Response.WriteAsync(WebhookResponse.AsErrorJson("type was not found."));
					return;
				}

				byte[] streamBytes = new byte[context.Request.ContentLength.Value];
				await context.Request.Body.ReadAsync(streamBytes, 0, streamBytes.Length);
				string json = HttpUtility.UrlDecode(Encoding.UTF8.GetString(streamBytes));

				Log.Debug($"Webhook accepted with type '{type}' with data '{json}'.");


				try
				{
                    await ev.OnMessage(json, services);
				}
				catch(Exception e)
				{
                    SentrySdk.CaptureException(e);
					Log.Error(e);
				}

				context.Response.ContentType = "application/json";
				await context.Response.WriteAsync(WebhookResponse.AsSuccessJson());
			});
		}
	}
}
