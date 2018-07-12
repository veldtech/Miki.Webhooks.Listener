using Miki.Configuration;
using Miki.Models;
using Miki.Rest;
using Miki.Webhooks.Listener.Exceptions;
using Miki.Webhooks.Listener.Models;
using Newtonsoft.Json;
using StackExchange.Redis.Extensions.Core;
using StatsdClient;
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
		public float DonatorModifier { get; set; } = 2.0f;

        [Configurable]
        public string ApiUrl { get; set; } = "localhost";

        [Configurable]
        public string ApiAuthorization { get; set; } = "password";

		private ICacheClient redisClient;

        public string[] AcceptedAuthCodes 
			=> new[]{ "DBL_VOTE" };

		public DblVoteEvent(ICacheClient cacheClient)
		{
			redisClient = cacheClient;
		}

		public async Task OnMessage(WebhookResponse response)
		{
			using (var context = new MikiContext())
			{
				DblVoteObject voteObject = response.data.ToObject<DblVoteObject>();

				if (voteObject.Type == "upvote")
				{
					User u = await context.Users.FindAsync(voteObject.UserId);

					if (!await redisClient.ExistsAsync($"dbl:vote:{voteObject.UserId}"))
					{
						u.DblVotes++;
						await redisClient.AddAsync($"dbl:vote:{voteObject.UserId}", 1, new TimeSpan(1, 0, 0, 0));

						int addedCurrency = 100 * ((await u.IsDonatorAsync(context)) ? 2 : 1);

						u.Currency += addedCurrency;

						DogStatsd.Increment("votes.dbl");

						Achievement achievement = await context.Achievements.FindAsync("voter", u.Id);
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

						await context.SaveChangesAsync();
					}
				}
			}

			var client = new RestClient(ApiUrl)
				.SetAuthorization(ApiAuthorization);

			await client.PostAsync("api/users/121919449996460033/messages", "{\"content\": \"yo.\"}");
		}
	}
}
