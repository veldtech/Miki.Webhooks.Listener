using Miki.Configuration;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Webhooks.Listener
{
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

		public async Task OnMessage(string json)
		{
			if(json.StartsWith("data="))
			{
				json = json.Substring(5);
			}

			KofiObject kofi = JsonConvert.DeserializeObject<KofiObject>(json);
			int rewardedKeys = (int)Math.Floor(kofi.Amount / 3);

			if (ulong.TryParse(kofi.Message.Split(' ').Last(), out ulong uid))
			{

				using (var context = new WebhookContext())
				{
					while (rewardedKeys > 0)
					{
						List<string> keys = new List<string>();

						for (int i = 0; i < Math.Min(10, rewardedKeys); i++)
						{
							var key = (await context.DonatorKey.AddAsync(new DonatorKey
							{
								StatusTime = TimeSpan.FromDays(31)
							})).Entity;

							await context.SaveChangesAsync();

							keys.Add(key.Key.ToString());
						}

						rewardedKeys -= keys.Count;

						var channel = await Program.Discord.CreateDMChannelAsync(uid);
						await Program.Discord.SendMessageAsync(channel.Id, new MessageArgs
						{
							embed = new EmbedBuilder()
							{
								Title = "🎉 You donated through Kofi!",
								Description = "From the bottom of my heart, I want to thank you for supporting my hobby and my passion project!\n\nWith love, Veld#0001"
							}.SetColor(221, 46, 68)
							.AddField("Here are your key(s)!", "\n```\n" + string.Join("\n", keys) + "```")
							.AddField("How to redeem this key?", $"use this command `>redeemkey`")
							.ToEmbed()
						});
						await Task.Delay(1000);
					}
				}
			}
		}
	}
}
