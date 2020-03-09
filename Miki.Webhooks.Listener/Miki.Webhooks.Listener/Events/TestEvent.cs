using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Webhooks.Listener.Events
{
	public class TestEvent : IWebhookEvent
	{
		public string[] AcceptedUrls => new[] { "test" };

		public Task OnMessage(string json, IServiceProvider _)
		{
			Console.WriteLine(json);
            return Task.CompletedTask;
		}
	}
}
