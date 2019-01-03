using Miki.Cache;
using Miki.Cache.StackExchange;
using Miki.Discord.Rest;
using Miki.Logging;
using Miki.Serialization.Protobuf;
using Miki.Webhooks.Listener.Events;
using Newtonsoft.Json;
using Sentry;
using StackExchange.Redis;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Miki.Webhooks.Listener
{
	class Program
    {
		public static WebhookConfiguration Configurations;
		public static DiscordApiClient Discord;

		public static async Task Main(string[] args)
		{
			if(!File.Exists("./database.json"))
			{
				using (var sw = File.CreateText("./database.json"))
				{
					string s = JsonConvert.SerializeObject(new WebhookConfiguration());
					await sw.WriteAsync(s);
					sw.Close();
				}
			}

			Configurations = JsonConvert.DeserializeObject<WebhookConfiguration>(await File.ReadAllTextAsync("./database.json"));

            using (SentrySdk.Init(Configurations.SentryDsn))
            {
                new LogBuilder()
                    .SetLogHeader((lvl) => $"[{lvl}][{DateTime.UtcNow.ToLongTimeString()}]")
                    .AddLogEvent((msg, level) => Console.WriteLine(msg))
                    .Apply();

                WebhookServer server = new WebhookServer(Configurations.AuthenticationString);

                ICacheClient redisClient = new StackExchangeCacheClient(
                    new ProtobufSerializer(),
                    await ConnectionMultiplexer.ConnectAsync(Configurations.RedisConnectionString)
                );

                Discord = new DiscordApiClient(Configurations.BotToken, redisClient);

                server.AddWebhookRoute(new TestEvent());
                server.AddWebhookRoute(new PatreonPaymentEvent());
                server.AddWebhookRoute(new DblVoteEvent(redisClient));
                server.AddWebhookRoute(new KofiPaymentEvent());

                await server.RunAsync(Configurations.Urls);
            }
		}
	}
}