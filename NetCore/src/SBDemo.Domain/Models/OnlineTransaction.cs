using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBDemo.Domain.Models
{
    [Serializable]
    public class OnlineTransaction
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TransactionType Type { get; set; }
        [JsonProperty("no")]
        public string No { get; set; }
        [JsonProperty("createdTimeUTC")]
        public DateTimeOffset CreatedTimeUTC { get; set; }
        [JsonProperty("customerId")]
        public string CustomerId { get; set; }
        [JsonProperty("total")]
        public decimal Total { get; set; }
        [JsonProperty("processedTimeUTC")]
        public DateTimeOffset? ProcessedTimeUTC { get; set; }
        [JsonProperty("processed")]
        public bool Processed { get; set; }
        [JsonProperty("items")]
        public Item[] Items { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
