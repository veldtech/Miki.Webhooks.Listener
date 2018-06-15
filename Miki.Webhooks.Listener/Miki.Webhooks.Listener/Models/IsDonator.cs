using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models
{
    public class IsDonator
    {
		public long UserId { get; set; }

		public int TotalPaidCents { get; set; }
		public int CurrentBalance { get; set; }
		public DateTime ValidUntil { get; set; }
		public int KeysRedeemed { get; set; }
	}
}
