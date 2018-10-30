using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Webhooks.Listener
{
	public class WebhookConfiguration
	{
		public string RedisConnectionString { get; set; } = "localhost";
		public string DatabaseConnectionString { get; set; } = "Server=localhost;";
		public string AuthenticationString { get; set; } = "password";
		public string BotToken { get; set; } = "";
	}
}
