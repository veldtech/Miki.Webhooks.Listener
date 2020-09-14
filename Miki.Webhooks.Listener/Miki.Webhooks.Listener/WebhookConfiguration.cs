namespace Miki.Webhooks.Listener
{
    public class WebhookConfiguration
	{
        public WebhookConfiguration(string redisConnectionString, string databaseConnectionString, string authenticationString, string sentryDsn, string botToken)
        {
            RedisConnectionString = redisConnectionString;
            DatabaseConnectionString = databaseConnectionString;
            AuthenticationString = authenticationString;
            SentryDsn = sentryDsn;
            BotToken = botToken;
        }

        public string RedisConnectionString { get; set; }
		public string DatabaseConnectionString { get; set; }
		public string AuthenticationString { get; set; }
        public string SentryDsn { get; set; }
		public string BotToken { get; set; }
	}
}
