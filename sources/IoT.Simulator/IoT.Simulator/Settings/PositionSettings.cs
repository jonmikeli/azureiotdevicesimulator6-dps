using Newtonsoft.Json;

using System;

namespace IoT.Simulator.Settings
{
    public class PositionSettings
    {
        [JsonProperty("positionType")]
        public string PositionType { get; set; }

        [JsonProperty("initialPosition")]
        public Position InitialPosition { get; set; }
    }

    public class Position
    {
        [JsonProperty("latitude")]
        public decimal Latitude { get; set; }
        [JsonProperty("longitude")]        
        public decimal Longitude { get; set; }
        [JsonProperty("altitude")]
        public decimal Altitude { get; set; }
        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
}
