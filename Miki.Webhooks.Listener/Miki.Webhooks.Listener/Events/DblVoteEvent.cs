namespace Miki.Webhooks.Listener.Events
{
    using Miki.Cache;
    using Miki.Configuration;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Logging;
    using Miki.Models;
    using Newtonsoft.Json;
    using ProtoBuf;
    using System;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Bot.Models;

    public class DblVoteObject
    {
        [JsonProperty("bot")] 
        public ulong BotId { get; set; }

        [JsonProperty("user")] 
        public ulong UserId { get; set; }

        [JsonProperty("type")] 
        public string Type { get; set; }
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

        public string[] AcceptedUrls => new[] {"dbl_vote"};

        public async Task OnMessage(string json, IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var service = scope.ServiceProvider;

            await using var context = service.GetService<DbContext>();

            var voteObject = JsonConvert.DeserializeObject<DblVoteObject>(json);
            if(voteObject.Type == "upvote")
            {
                var user = await context.Set<User>().FindAsync((long) voteObject.UserId);
                user.DblVotes++;

                var cacheClient = service.GetService<ICacheClient>();

                StreakObject streakObj =
                    await cacheClient.GetAsync<StreakObject>($"dbl:vote:{voteObject.UserId}");
                if(streakObj == null)
                {
                    streakObj = new StreakObject
                    {
                        Streak = 0, 
                        TimeStreak = DateTime.MinValue
                    };
                }

                if(streakObj.TimeStreak > DateTime.UtcNow.AddHours(-11))
                {
                    Log.Warning($"Event of type {nameof(DblVoteEvent)} rejected.");
                    return;
                }

                streakObj.Streak++;
                streakObj.TimeStreak = DateTime.UtcNow;

                await cacheClient.UpsertAsync(
                    $"dbl:vote:{voteObject.UserId}", streakObj, new TimeSpan(24, 0, 0));

                var isDonator = await user.IsDonatorAsync(context);

                int addedCurrency = 100 * (isDonator ? DonatorModifier : 1) 
                                        * Math.Min(100, streakObj.Streak);
                user.Currency += addedCurrency;

                switch(user.DblVotes)
                {
                    case 1:
                    {
                        await context.AddAsync(new Achievement
                        {
                            Name = "voter",
                            Rank = 0,
                            UnlockedAt = DateTime.Now,
                            UserId = user.Id
                        });
                    }
                        break;
                    case 25:
                    {
                        await context.AddAsync(new Achievement
                        {
                            Name = "voter",
                            Rank = 1,
                            UnlockedAt = DateTime.Now,
                            UserId = user.Id
                        });
                    }
                        break;
                    case 200:
                    {
                        await context.AddAsync(new Achievement
                        {
                            Name = "voter",
                            Rank = 2,
                            UnlockedAt = DateTime.Now,
                            UserId = user.Id
                        });
                    }
                        break;
                }

                var apiClient = service.GetService<IApiClient>();
                var channel = await apiClient.CreateDMChannelAsync(voteObject.UserId);
                if(channel != null)
                {
                    await apiClient.SendMessageAsync(channel.Id,
                        new MessageArgs
                        {
                            Embed = new EmbedBuilder()
                                .SetTitle("🎉 Thank you for voting!")
                                .SetDescription(
                                    $"You have been granted 🔸{addedCurrency}\n{(streakObj.Streak > 1 ? $"🔥 You're on a {streakObj.Streak} day streak!" : "")}")
                                .SetColor(221, 46, 68)
                                .ToEmbed()
                        });
                }

                await context.SaveChangesAsync();
            }
        }
    }
}