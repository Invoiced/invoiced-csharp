using Newtonsoft.Json;

namespace Invoiced
{
    public class CreditNote : AbstractDocument<CreditNote>
    {
        internal CreditNote()
        {
            EntityName = "/credit_notes";
        }

        internal CreditNote(Connection conn) : base(conn)
        {
            EntityName = "/credit_notes";
        }

        [JsonProperty("paid")] public bool? Paid { get; set; }

        [JsonProperty("balance")] public double? Balance { get; set; }

        // Conditional Serialisation
        public bool ShouldSerializePaid()
        {
            return false;
        }

        public bool ShouldSerializeBalance()
        {
            return false;
        }
    }
}