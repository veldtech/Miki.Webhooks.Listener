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

namespace Miki.Webhooks.Listener
{
	public class WebhookServer
	{
		public class WebhookResponse
		{
			[JsonProperty("success")]
			public bool Success = false;

			[JsonProperty("error")]
			public string Error = null;

			public static WebhookResponse AsError(string errorMessage)
				=> new WebhookResponse { Error = errorMessage };
			public static string AsErrorJson(string errorMessage)
				=> JsonConvert.SerializeObject(AsError(errorMessage));

			public static WebhookResponse AsSuccess()
				=> new WebhookResponse { Success = true };
			public static string AsSuccessJson()
				=> JsonConvert.SerializeObject(AsSuccess());
		}

		private Dictionary<string, IWebhookEvent> _allWebhookEvents= new Dictionary<string, IWebhookEvent>();
		private ConfigurationManager _configuration = new ConfigurationManager();

		private readonly string AuthKey = null;

		public WebhookServer()
		{
		}
		public WebhookServer(string key)
		{
			AuthKey = key;
		}

		public void AddWebhookRoute(IWebhookEvent ev)
		{
			foreach (var x in ev.AcceptedUrls)
			{
				_allWebhookEvents.Add(x, ev);
			}
		}

		public async Task RunAsync(string[] urls)
		{
			if (File.Exists("./config.json"))
			{
				await _configuration.ImportAsync(
					new JsonSerializationProvider(),
					"./config.json");
			}

			await _configuration.ExportAsync(
				new JsonSerializationProvider(),
				"./config.json");

			var host = new WebHostBuilder()
				.UseKestrel()
				.UseUrls(urls)
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

				string type = null;

				if (!context.Request.Query.TryGetValue("type", out var value))
				{
					await context.Response.WriteAsync(WebhookResponse.AsErrorJson("no webhook type defined."));
					return;
				}

				if (AuthKey != null)
				{
					if (!context.Request.Query.TryGetValue("key", out var auth))
					{
						await context.Response.WriteAsync(WebhookResponse.AsErrorJson("no authorization provided."));
						return;
					}

					if(auth != AuthKey)
					{
						await context.Response.WriteAsync(WebhookResponse.AsErrorJson("unauthorized."));
						return;
					}
				}

				type = value.FirstOrDefault();

				if(!_allWebhookEvents.TryGetValue(type, out var ev))
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
					await ev.OnMessage(json);
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
