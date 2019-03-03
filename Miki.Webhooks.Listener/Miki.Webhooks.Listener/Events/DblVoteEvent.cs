using Miki.Cache;
using Miki.Configuration;
using Miki.Discord;
using Miki.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
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

    [ProtoContract]
    public class StreakObject
    {
        [ProtoMember(1)]
        public DateTime TimeStreak { get; set; } = DateTime.Now;

        [ProtoMember(2)]
        public int Streak { get; set; } = 1;
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
                    u.DblVotes++;

                    StreakObject streakObj = await redisClient.GetAsync<StreakObject>($"dbl:vote:{voteObject.UserId}");
                    if (streakObj == null)
                    {
                        streakObj = new StreakObject { Streak = 0, TimeStreak = DateTime.MinValue }; 
                    }

                    if (streakObj.TimeStreak < DateTime.UtcNow.AddHours(-11))
                    {
                        return;
                    }

                    streakObj.Streak++;
                    streakObj.TimeStreak = DateTime.UtcNow;

                    await redisClient.UpsertAsync($"dbl:vote:{voteObject.UserId}", streakObj, new TimeSpan(24, 0, 0));

                    int addedCurrency = (100 * (await u.IsDonatorAsync(context) ? DonatorModifier : 1)) * Math.Min(100, streakObj.Streak);

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
                                UserId = u.Id
                            };
                            unlockedAchievement = true;
                        } break;
                        case 25:
                        {
                            achievement.Rank = 1;
                            unlockedAchievement = true;
                        } break;
                        case 200:
                        {
                            achievement.Rank = 2;
                            unlockedAchievement = true;
                        } break;
                    }

                    var channel = await Program.Discord.CreateDMChannelAsync(voteObject.UserId);
                    if (channel != null)
                    {
                        await Program.Discord.SendMessageAsync(channel.Id, new Discord.Common.MessageArgs
                        {
                            embed = new EmbedBuilder()
                                .SetTitle("🎉 Thank you for voting!")
                                .SetDescription($"You have been granted 🔸{addedCurrency}\n{(streakObj.Streak > 1 ? $"🔥 You're on a {streakObj.Streak} day streak!" : "")}")
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
