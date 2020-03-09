namespace Miki.Webhooks.Listener
{
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Configuration;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;

    public class KofiPaymentEvent : IWebhookEvent
	{
        public class KofiObject
		{
			[JsonProperty("message_id")]
			public Guid MessageId;

			[JsonProperty("timestamp")]
			public DateTimeOffset Timestamp;

			[JsonProperty("type")]
			public string Type;

			[JsonProperty("from_name")]
			public string FromName;

			[JsonProperty("message")]
			public string Message;

			[JsonProperty("amount")]
			public double Amount;

			[JsonProperty("url")]
			public string Url;
		}

		[Configurable]
		public int PricePerKey { get; set; } = 3;

		public string[] AcceptedUrls => new[] { "kofi" };

        public async Task OnMessage(string json, IServiceProvider services)
		{
			if(json.StartsWith("data="))
			{
				json = json.Substring(5);
			}

			KofiObject kofi = JsonConvert.DeserializeObject<KofiObject>(json);
			int rewardedKeys = (int)Math.Floor(kofi.Amount / 3);

			if (ulong.TryParse(kofi.Message.Split(' ').Last(), out ulong uid))
            {
                await using var context = services.GetService<DbContext>();

                while(rewardedKeys > 0)
                {
                    List<string> keys = new List<string>();

                    for (int i = 0; i < Math.Min(10, rewardedKeys); i++)
                    {
                        var key = (await context.Set<DonatorKey>().AddAsync(new DonatorKey
                        {
                            StatusTime = TimeSpan.FromDays(31),
                            Key = Guid.NewGuid()
                        })).Entity;

                        await context.SaveChangesAsync();

                        keys.Add(key.Key.ToString());
                    }

                    rewardedKeys -= keys.Count;

                    var apiClient = services.GetService<IApiClient>();
                    var channel = await apiClient.CreateDMChannelAsync(uid);
                    await apiClient.SendMessageAsync(channel.Id,
                        new MessageArgs
                        {
                            Embed = new EmbedBuilder()
                                {
                                    Title = "🎉 You donated through Kofi!",
                                    Description =
                                        "From the bottom of my heart, I want to thank you for supporting my hobby and my passion project!\n\nWith love, Veld#0001"
                                }.SetColor(221, 46, 68)
                                .AddField("Here are your key(s)!",
                                    "\n```\n" + string.Join("\n", keys) + "```")
                                .AddField("How to redeem this key?", "use this command `>redeemkey`")
                                .ToEmbed()
                        });
                    await Task.Delay(2000);
                }
            }
		}
	}
}
