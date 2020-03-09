namespace Miki.Webhooks.Listener
{
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
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Bot.Models;
    using Miki.Discord.Common;
    using Miki.Serialization;

    class Program
    {
		public static WebhookConfiguration Configurations;

		public static async Task Main(string[] args)
		{
			if(!File.Exists("./database.json"))
            {
                await using var sw = File.CreateText("./database.json");
                string s = JsonConvert.SerializeObject(new WebhookConfiguration());
                await sw.WriteAsync(s);
                sw.Close();
            }
            Configurations = JsonConvert.DeserializeObject<WebhookConfiguration>(await File.ReadAllTextAsync("./database.json"));

            new LogBuilder()
                .SetLogHeader((lvl) => $"[{lvl}][{DateTime.UtcNow.ToLongTimeString()}]")
                .AddLogEvent((msg, level) => Console.WriteLine(msg))
                .Apply();

            using (SentrySdk.Init(Configurations.SentryDsn))
            {
                ServiceCollection collection = new ServiceCollection();
                collection.AddDbContext<DbContext, MikiDbContext>(
                    x => x.UseNpgsql(Configurations.DatabaseConnectionString));
                collection.AddSingleton(x => new WebhookServer(Configurations.AuthenticationString, x));
                collection.AddSingleton<ISerializer, ProtobufSerializer>();
                collection.AddSingleton<IConnectionMultiplexer>(
                    await ConnectionMultiplexer.ConnectAsync(Configurations.RedisConnectionString));
                collection.AddSingleton<ICacheClient, StackExchangeCacheClient>();
                collection.AddSingleton<IApiClient>(
                    x => new DiscordApiClient(Configurations.BotToken, x.GetService<ICacheClient>()));
                var provider = collection.BuildServiceProvider();

                var server = provider.GetService<WebhookServer>();
                server.AddWebhookRoute(new TestEvent());
                server.AddWebhookRoute(new PatreonPaymentEvent());
                server.AddWebhookRoute(new DblVoteEvent());
                server.AddWebhookRoute(new KofiPaymentEvent());
                await server.RunAsync(Configurations.Urls);
            }
		}
	}
}