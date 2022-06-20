using Newtonsoft.Json;

using System;

namespace IoT.Simulator.Models
{
    internal class CommissioningRequest
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("messageType")]
        public string MessageType => "commissioning";

        [JsonProperty("messageDateTime")]
        public DateTime MessageDateTime { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("companyId")]
        public string CompanyId { get; set; }

        [JsonProperty("resourceTypeId")]
        public string ResourceTypeId { get; set; }
    }
}
