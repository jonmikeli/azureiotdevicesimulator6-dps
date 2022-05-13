using Newtonsoft.Json;

using System;

namespace IoT.Simulator.Models
{
    public class CustomStatusTelemetryTro
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; }

        [JsonProperty("benchSerialNumber")]
        public string BenchSerialNumber { get; set; }

        [JsonProperty("statusDateTime")]
        public DateTime StatusDateTime { get; set; }

        [JsonProperty("newStatus")]
        public string NewStatus { get; set; }

        [JsonProperty("oldStatus")]
        public string oldStatus { get; set; }
        
        [JsonProperty("messageType")]
        public string MessageType { get; set; }
        
        [JsonProperty("category")]
        public string Category { get; set; }
    }

    public class CustomHeaderTro
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; }
        
        [JsonProperty("messageType")]
        public string MessageType { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }
        
    }

}
