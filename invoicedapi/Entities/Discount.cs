using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Invoiced
{

	public class Discount : Item
	{
		public Discount() : base(){

		}

		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("object")]
		public string Obj { get; set; }

		[JsonProperty("amount")]
		public double Amount { get; set; }

		[JsonProperty("coupon")]
		public Coupon Coupon { get; set; }

		[JsonProperty("expires")]
		public long Expires { get; set; }

		override public long EntityID() {
			return this.Id;
		}

	}

}