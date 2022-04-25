using Newtonsoft.Json;

using System;

namespace IoT.Simulator.Models
{
    public class CustomTelemetryTit
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("moduleId")]
        public string ModuleId { get; set; }

        [JsonProperty("messageDateTime")]
        public DateTime MessageDateTime { get; set; }

        [JsonProperty("messageType")]
        public string MessageType { get; set; }

        [JsonProperty("batteryLevels")]
        public BatteryData[] BatteryLevels { get; set; }

        [JsonProperty("tankLevels")]
        public TankData[] TankLevels { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }

        [JsonProperty("status")]
        public Status Status { get; set; }
    }

    public class Position
    {
        [JsonProperty("latitude")]
        public decimal Latitude { get; set; }

        [JsonProperty("longitude")]
        public decimal Longitude { get; set; }

        [JsonProperty("positionDateTime")]
        public DateTime PositionDateTime { get; set; }
    }

    public class BatteryData
    {
        [JsonProperty("batteryId")]
        public string BatteryId { get; set; }

        [JsonProperty("batteryLevel")]
        public decimal BatteryLevel { get; set; }

        [JsonProperty("batteryLevelDateTime")]
        public DateTime BatteryLevelDateTime { get; set; }
    }

    public class TankData
    {
        [JsonProperty("tankId")]
        public string TankId { get; set; }

        [JsonProperty("tankLevel")]
        public decimal TankLevel { get; set; }

        [JsonProperty("tankLevelDateTime")]
        public DateTime TankLevelDateTime { get; set; }
    }

    public class Status
    {
        [JsonProperty("statusValue")]
        public string StatusValue { get; set; }

        [JsonProperty("statusDateTime")]
        public DateTime StatusDateTime { get; set; }
    }

}
