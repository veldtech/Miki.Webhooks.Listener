using System.Threading.Tasks;

namespace Miki.Webhooks.Listener
{
    public interface IWebhookEvent
    {
		string[] AcceptedAuthCodes { get; }

		Task OnMessage(WebhookResponse response);
    }
}