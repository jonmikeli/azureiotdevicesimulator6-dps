using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoT.Simulator.Models
{
    public class CustomTelemetryTit
    {
        [JsonProperty("schema")]
        public string Schema { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("        moduleId")]
        public string ModuleId { get; set; }

        [JsonProperty("messageDateTime")]
        public DateTime MessageDateTime { get; set; }

        [JsonProperty("messageType")]        
        public string MessageType { get; set; }

        [JsonProperty("batteryLevels")]
        public Batterylevel[] BatteryLevels { get; set; }

        [JsonProperty("tankLevels")]
        public Tanklevel[] TankLevels { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class Position
    {
        [JsonProperty("latitude")]
        public string Latitude { get; set; }

        [JsonProperty("longitude")]
        public string Longitude { get; set; }

        [JsonProperty("positionDateTime")]
        public DateTime PositionDateTime { get; set; }
    }

    public class Batterylevel
    {
        [JsonProperty("batteryId")]
        public string BatteryId { get; set; }

        [JsonProperty("batteryLevel")]
        public string BatteryLevel { get; set; }

        [JsonProperty("batteryLevelDateTime")]
        public DateTime BatteryLevelDateTime { get; set; }
    }

    public class Tanklevel
    {
        [JsonProperty("tankId")]
        public string TankId { get; set; }
        
        [JsonProperty("tankLevel")]
        public string TankLevel { get; set; }

        [JsonProperty("tankLevelDateTime")]
        public DateTime TankLevelDateTime { get; set; }
    }

}
