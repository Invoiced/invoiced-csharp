using Newtonsoft.Json;

namespace Invoiced
{
    public class EmailRequest : AbstractItem
    {
        [JsonProperty("to")] public EmailRecipient[] To { get; set; }

        [JsonProperty("bcc")] public string Bcc { get; set; }

        [JsonProperty("subject")] public string Subject { get; set; }

        [JsonProperty("message")] public string Message { get; set; }
        
        [JsonProperty("template")] public string Template {get; set;}

        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("start")] public long? Start { get; set; }

        [JsonProperty("end")] public long? End { get; set; }

        [JsonProperty("items")] public string Items { get; set; }

        protected override string EntityId()
        {
            return "EmailRequest";
            // this is only used for json heading in ToString()
        }
    }
}
