using System.Threading.Tasks;

namespace Miki.Webhooks.Listener
{
    using System;

    public interface IWebhookEvent
    {
		string[] AcceptedUrls { get; }

		Task OnMessage(string json, IServiceProvider provider);
    }
}