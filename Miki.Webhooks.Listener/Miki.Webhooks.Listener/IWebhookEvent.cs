using System.Threading.Tasks;

namespace Miki.Webhooks.Listener
{
    public interface IWebhookEvent
    {
		string[] AcceptedUrls { get; }

		Task OnMessage(string json);
    }
}