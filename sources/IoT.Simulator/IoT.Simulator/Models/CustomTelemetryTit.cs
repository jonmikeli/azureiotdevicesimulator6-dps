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

    public class CustomFuelingTelemetryTit
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

        [JsonProperty("value")]
        public FuelingSession Value { get; set; }        
    }

    public class FuelingSession
    {
        [JsonProperty("startDateTime")]
        public DateTime StartDateTime { get; set; }
        [JsonProperty("duration")]
        public int Duration { get; set; }
        [JsonProperty("dataDateTime")]
        public DateTime DataDateTime { get; set; }
        [JsonProperty("position")]
        public Position Position { get; set; }
        [JsonProperty("pressure")]
        public decimal Pressure { get; set; }
        [JsonProperty("netVolumeDelivered")]
        public decimal NetVolumeDelivered { get; set; }
        [JsonProperty("averageFlowRate")]
        public decimal AverageFlowRate { get; set; }
        [JsonProperty("regularPressure")]
        public decimal RegularPressure { get; set; }
        [JsonProperty("interlockOverride")]
        public bool InterlockOverride { get; set; }
        [JsonProperty("batteriesLevelsAtTheBeginning")]
        public BatteryData[] BatteryLevelsAtStart { get; set; }
        [JsonProperty("batteriesLevelsAtTheEnd")]
        public BatteryData[] BatteryLevelsAtEnd { get; set; }
        [JsonProperty("tankLevelsAtTheBeginning")]
        public TankData[] TankLevelsAtStart { get; set; }
        [JsonProperty("tankLevelsAtTheEnd")]
        public TankData[] TankLevelsAtEnd { get; set; }        
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
