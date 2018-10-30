using Miki.Configuration;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Webhooks.Listener.Services
{
	[Service]
    public class RedisClient
    {
		[Configurable]
		public string ConnectionString { get; set; } = "localhost";

		public StackExchangeRedisCacheClient redisClient;

		public RedisClient()
		{
		}

		public void Connect()
		{
			redisClient = new StackExchangeRedisCacheClient(new NewtonsoftSerializer(), ConnectionString);
		}
	}
}
