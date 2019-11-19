using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Invoiced
{

	public class Plan : AbstractEntity<Plan> {


		internal Plan(Connection conn) : base(conn) {
		}

		protected override string EntityIdString() {
			return this.Id;
		}

		public override string EntityName() {
			return "plans";
		}

		public override bool HasStringId() {
			return true;
		}

		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("object")]
		public string Obj { get; set; }

        [JsonProperty("catalog_item")]
		public string CatalogItem { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("currency")]
		public string Currency { get; set; }

		[JsonProperty("amount")]
		public long Amount { get; set; }

        [JsonProperty("pricing_mode")]
		public string PricingMode { get; set; }

        [JsonProperty("quantity_type")]
		public string QuantityType { get; set; }

        [JsonProperty("interval")]
		public string Interval { get; set; }

        [JsonProperty("interval_count")]
		public long IntervalCount { get; set; }

        [JsonProperty("tiers")]
		public object Tiers { get; set; }

		[JsonProperty("created_at")]
		public long CreatedAt { get; set; }

		[JsonProperty("metadata")]
		public Metadata Metadata { get; set; }

		// Conditional Serialisation

		public bool ShouldSerializeId() {
			return this.CurrentOperation == "Create";
		}

		public bool ShouldSerializeObj() {
			return false;
		}

		public bool ShouldSerializeCatalogItem() {
			return this.CurrentOperation == "Create";
		}

		public bool ShouldSerializeCurrency() {
			return this.CurrentOperation == "Create";
		}

		public bool ShouldSerializeAmount() {
			return this.CurrentOperation == "Create";
		}

		public bool ShouldSerializePricingMode() {
			return this.CurrentOperation == "Create";
		}

		public bool ShouldSerializeQuantityType() {
			return this.CurrentOperation == "Create";
		}

		public bool ShouldSerializeInterval() {
			return this.CurrentOperation == "Create";
		}

		public bool ShouldSerializeIntervalCount() {
			return this.CurrentOperation == "Create";
		}

		public bool ShouldSerializeTiers() {
			return this.CurrentOperation == "Create";
		}

	}

}
