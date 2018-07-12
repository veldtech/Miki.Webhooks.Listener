using Miki.Configuration;
using Miki.Logging;
using Miki.Webhooks.Listener.Events;
using RabbitMQ.Client;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Miki.Webhooks.Listener
{
    class Program
    {
		ConfigurationManager configuration = new ConfigurationManager();

		RabbitClient client = new RabbitClient();

		public StackExchangeRedisCacheClient redisClient;

		[Configurable]
		public string RedisUrl { get; set; } = "localhost";

		static void Main(string[] args)
        {
			new Program().MainAsync()
				.GetAwaiter().GetResult();
		}

		async Task MainAsync()
		{
			Log.OnLog += (msg, level) => Console.WriteLine(msg);

			configuration.RegisterType(this);
			configuration.RegisterType(client);

			redisClient = new StackExchangeRedisCacheClient(new NewtonsoftSerializer(), RedisUrl);

			AddWebhookEvent(client, new DblVoteEvent(redisClient));

			if (File.Exists("./config.json"))
			{
				await configuration.ImportAsync(
					new JsonSerializationProvider(),
					"./config.json");
			}

			await configuration.ExportAsync(
				new JsonSerializationProvider(),
				"./config.json");

			client.Connect();

			Log.Message("running!");
		}

		void AddWebhookEvent(RabbitClient client, IWebhookEvent ev)
		{
			configuration.RegisterType(ev.GetType(), ev);
			client.AddWebhookEvent(ev);
		}
	}
}
