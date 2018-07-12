using Miki.Configuration;
using Miki.Logging;
using Miki.Webhooks.Listener.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Webhooks.Listener
{
	public class WebhookResponse
	{
		[JsonProperty("auth_code")]
		public string auth_code;

		[JsonProperty("data")]
		public JToken data;
	}

	public class RabbitClient
	{
		[Configurable]
		public string RabbitUrl { get; set; } = "amqp://localhost";

		[Configurable]
		public string QueueName { get; set; } = "default";

		public Func<WebhookResponse, Task> OnMessage;

		private IConnection connection;
		private IModel model;

		private List<IWebhookEvent> webhookEvents = new List<IWebhookEvent>();

		public void AddWebhookEvent(IWebhookEvent ev)
		{
			webhookEvents.Add(ev);
		}

		public void Connect()
		{
			ConnectionFactory connFactory = new ConnectionFactory();

			connFactory.Uri = new Uri(RabbitUrl);

			connection = connFactory.CreateConnection();
			model = connection.CreateModel();

			model.ExchangeDeclare("miki", ExchangeType.Direct, true);
			model.QueueDeclare(QueueName, true, false, false, null);
			model.QueueBind(QueueName, "miki", "*", null);

			var consumer = new EventingBasicConsumer(model);
			consumer.Received += async (ch, ea) => await OnMessageReceived(ch, ea);
			string consumerTag = model.BasicConsume(QueueName, false, consumer);
		}

		private async Task OnMessageReceived(object channel, BasicDeliverEventArgs args)
		{
			WebhookResponse resp = null;

			try
			{
				string payload = Encoding.UTF8.GetString(args.Body);
				resp = JsonConvert.DeserializeObject<WebhookResponse>(payload);
			}
			catch (Exception e)
			{
				Log.Error(e);
				model.BasicReject(args.DeliveryTag, false);
				return;
			}

			Log.Message(resp.auth_code + " | received");


			try
			{
				foreach (IWebhookEvent webhook in webhookEvents)
				{
					if (webhook.AcceptedAuthCodes.Contains(resp.auth_code))
					{
						await webhook.OnMessage(resp);
						Log.Message($"processed packet with auth_code of '{resp.auth_code}' to {webhook.GetType().Name}");
					}
				}

				if (OnMessage != null)
				{
					await OnMessage(resp);
				}

				model.BasicAck(args.DeliveryTag, false);
			}
			catch (RabbitException e)
			{
				var prop = model.CreateBasicProperties();
				prop.Headers = new Dictionary<string, object>();

				if (args.BasicProperties.Headers.TryGetValue("x-retry-count", out object value))
				{
					int rCount = int.Parse(value.ToString() ?? "0") + 1;

					prop.Headers.Add("x-retry-count", rCount);

					if (rCount > 10)
					{
						return;
					}
				}
				else
				{
					prop.Headers.Add("x-retry-count", 1);
				}
				model.BasicPublish("miki", "*", false, prop, args.Body);
			}
			catch (Exception e)
			{
				Log.Error(e);
				model.BasicAck(args.DeliveryTag, false);
			}
		}
	}
}
