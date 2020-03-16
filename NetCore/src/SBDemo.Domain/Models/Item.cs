using Newtonsoft.Json;
using System;

namespace SBDemo.Domain.Models
{
    [Serializable]
    public class Item
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("qty")]
        public int Qty { get; set; }
        [JsonProperty("price")]
        public decimal Price { get; set; }
    }
}
