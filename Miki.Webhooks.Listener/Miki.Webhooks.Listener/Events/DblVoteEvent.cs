using Miki.Cache;
using Miki.Configuration;
using Miki.Discord;
using Miki.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Webhooks.Listener.Events
{
	public class DblVoteObject
	{
		[JsonProperty("bot")]
		public ulong BotId;

		[JsonProperty("user")]
		public ulong UserId;

		[JsonProperty("type")]
		public string Type;
	}

	public class DblVoteEvent : IWebhookEvent
	{
		[Configurable]
		public int MekosGiven { get; set; } = 100;

		[Configurable]
		public int DonatorModifier { get; set; } = 2;

		public string[] AcceptedUrls => new[] { "dbl_vote" };

		private ICacheClient redisClient;

		public DblVoteEvent(ICacheClient cacheClient)
		{
			redisClient = cacheClient;
		}

		public async Task OnMessage(string json)
		{
			using (var context = new WebhookContext())
			{
				DblVoteObject voteObject = JsonConvert.DeserializeObject<DblVoteObject>(json);

				if (voteObject.Type == "upvote")
				{
					User u = await context.Users.FindAsync((long)voteObject.UserId);

					if (await redisClient.ExistsAsync($"dbl:vote:{voteObject.UserId}"))
					{
						u.DblVotes++;
						await redisClient.UpsertAsync($"dbl:vote:{voteObject.UserId}", 1, new TimeSpan(1, 0, 0, 0));

						int addedCurrency = 100 * (await u.IsDonatorAsync(context) ? DonatorModifier : 1);

						u.Currency += addedCurrency;

						Achievement achievement = await context.Achievements.FindAsync(u.Id, "voter");
						bool unlockedAchievement = false;

						switch (u.DblVotes)
						{
							case 1:
							{
								achievement = new Achievement()
								{
									Name = "voter",
									Rank = 0,
									UnlockedAt = DateTime.Now,
									Id = u.Id
								};
								unlockedAchievement = true;
							}
							break;
							case 25:
							{
								achievement.Rank = 1;
								unlockedAchievement = true;
							}
							break;
							case 200:
							{
								achievement.Rank = 2;
								unlockedAchievement = true;
							}
							break;
						}

						var channel = await Program.Discord.CreateDMChannelAsync(voteObject.UserId);
						if (channel != null)
						{
							await Program.Discord.SendMessageAsync(channel.Id, new Discord.Common.MessageArgs
							{
								embed = new EmbedBuilder()
									.SetTitle("🎉 Thank you for voting!")
									.SetDescription($"You have been granted 🔸{addedCurrency}")
									.SetColor(221, 46, 68)
									.ToEmbed()
							});
						}

						await context.SaveChangesAsync();
					}
				}
			}
		}
	}
}
