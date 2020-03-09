namespace Miki.Webhooks.Listener
{
    public class WebhookConfiguration
	{
		public string RedisConnectionString { get; set; } = "localhost";
		public string DatabaseConnectionString { get; set; } = "Server=localhost;";
		public string AuthenticationString { get; set; } = "password";
        public string SentryDsn { get; set; } = "";
		public string BotToken { get; set; } = "";
		public string[] Urls { get; set; } = new[] { "http://localhost:5000", "https://localhost:5001" };
	}
}
