namespace Miki.Webhooks.Listener.Events
{
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Logging;
    using Miki.Models;
    using Newtonsoft.Json;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;

    public class PatreonUserReward
    {
        [JsonProperty("user_id")]
        public ulong UserId { get; set; }

        [JsonProperty("keys_rewarded")]
        public int KeysRewarded { get; set; }
    }

    public class PatreonPaymentEvent : IWebhookEvent
    {
        private Task backgroundTask;

        public string[] AcceptedUrls => new[] {"patreon"};

        public Task OnMessage(string response, IServiceProvider services)
        {
            List<PatreonUserReward> rewards =
                JsonConvert.DeserializeObject<List<PatreonUserReward>>(response);
            if(backgroundTask?.Status == TaskStatus.Running)
            {
                SentrySdk.CaptureException(
                    new OperationCanceledException("Patreon payment event overridden."));
            }

            backgroundTask = ProcessPaymentsAsync(rewards, services);
            return Task.CompletedTask;
        }

        private async Task ProcessPaymentsAsync(
            IEnumerable<PatreonUserReward> paymentEvents,
            IServiceProvider services)
        {
            await using var context = services.GetService<DbContext>();
            foreach(var reward in paymentEvents)
            {
                while(reward.KeysRewarded > 0)
                {
                    List<string> keys = new List<string>();

                    for(int i = 0; i < Math.Min(10, reward.KeysRewarded); i++)
                    {
                        try
                        {
                            var key = (await context.Set<DonatorKey>().AddAsync(new DonatorKey
                            {
                                StatusTime = TimeSpan.FromDays(31),
                                Key = Guid.NewGuid()
                            })).Entity;

                            await context.SaveChangesAsync();
                            keys.Add(key.Key.ToString());
                        }
                        catch(Exception e)
                        {
                            Log.Error(e);
                        }
                    }

                    reward.KeysRewarded -= keys.Count;

                    try
                    {
                        var apiClient = services.GetService<IApiClient>();
                        var channel = await apiClient.CreateDMChannelAsync(reward.UserId);
                        await apiClient.SendMessageAsync(channel.Id, new MessageArgs
                        {
                            Embed = new EmbedBuilder()
                                .SetTitle("🎉 You donated through Patreon!")
                                .SetDescription(
                                    "From the bottom of my heart, I want to thank you for supporting my hobby and my passion project!\n\nWith love, Veld#0001")
                                .SetColor(221, 46, 68)
                                .AddField("Here are your key(s)!",
                                    "\n```\n" + string.Join("\n", keys) + "```")
                                .AddField("How to redeem this key?", 
                                    "use this command `>redeemkey` to get your donator privileges, or `>sellkey` to get mekos!")
                                .ToEmbed()
                        });
                    }
                    catch(Exception e)
                    {
                        SentrySdk.CaptureException(e);
                        Log.Error(e);
                    }

                    await Task.Delay(2000);
                }
            }
        }
    }
}