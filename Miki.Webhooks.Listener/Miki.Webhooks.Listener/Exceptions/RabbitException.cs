using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Webhooks.Listener.Exceptions
{
    public class RabbitException : Exception
    {
		public bool CanRetry { get; private set; }

		public RabbitException(string message, bool canRetry)
			: base(message)
		{
			CanRetry = canRetry;
		}
    }
}
