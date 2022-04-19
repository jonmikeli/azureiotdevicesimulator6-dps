﻿using IoT.Simulator.Exceptions;
using IoT.Simulator.Extensions;
using IoT.Simulator.Models;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoT.Simulator.Tools
{
    public class IoTTools
    {
        #region Tooling
        internal static void CheckDeviceConnectionStringData(string connectionString, ILogger logger)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "No IoT Hub connection string has been found");

            if (logger == null)
                throw new ArgumentNullException(nameof(logger), "No logger has been provided");

            string logPrefix = "system".BuildLogPrefix();

            //Hostname
            string hostname = connectionString.ExtractValue("HostName");
            if (string.IsNullOrEmpty(hostname))
                throw new ConnectionStringException("hostname", "No hostname has been found within the connection string");

            logger.LogDebug($"{logPrefix}::IoT Hub hostname: {hostname}");

            //DeviceId
            string deviceId = connectionString.ExtractValue("DeviceId");
            if (string.IsNullOrEmpty(deviceId))
                throw new ConnectionStringException("DeviceId", "No deviceId has been found within the connection string");

            logger.LogDebug($"{logPrefix}::DeviceId:{deviceId}::CheckDeviceConnectionStringData");
        }

        internal static void CheckModuleConnectionStringData(string connectionString, ILogger logger)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "No IoT Hub connection string has been found");

            if (logger == null)
                throw new ArgumentNullException(nameof(logger), "No logger has been provided");

            string logPrefix = "system".BuildLogPrefix();

            CheckDeviceConnectionStringData(connectionString, logger);

            //ModuleId
            string moduleId = connectionString.ExtractValue("ModuleId");
            if (string.IsNullOrEmpty(moduleId))
                throw new ConnectionStringException("No hostname has been found within the connection string", nameof(moduleId));

            logger.LogDebug($"{logPrefix}::ModuleId:{moduleId}::CheckModuleConnectionStringData");
        }

        internal static string RandomizeData(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage))
                throw new ArgumentNullException(nameof(jsonMessage));

            JObject jobject = JObject.Parse(jsonMessage);

            if (jobject != null)
            {
                JToken jData;

                if (jobject.TryGetValue("data", out jData) && jData.Type == JTokenType.Array)
                {
                    Random r = new Random(DateTime.Now.Second);
                    foreach (var item in (JArray)jData)
                    {
                        item["timestamp"] = JValue.FromObject(DateTime.Now.TimeStamp());
                        item["propertyValue"] = JValue.FromObject(r.Next(150, 300).ToString());
                    }
                }

                return JsonConvert.SerializeObject(jobject, Formatting.Indented);
            }
            else return null;
        }

        internal static string RandomizeCustomData(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage))
                throw new ArgumentNullException(nameof(jsonMessage));

            CustomTelemetryTit data = JsonConvert.DeserializeObject<CustomTelemetryTit>(jsonMessage);

            if (data != null)
            {
                Random r = new Random(DateTime.Now.Second);
                DateTime messageConstructionDate = DateTime.UtcNow;

                if (data.Position != null)
                {
                    data.Position.Latitude = data.Position.Latitude + (decimal)r.Next(5, 20) / 10;
                    data.Position.Longitude = data.Position.Longitude + (decimal)r.Next(5, 20) / 10;
                    data.Position.PositionDateTime = messageConstructionDate;
                }

                data.MessageDateTime = messageConstructionDate;
                data.BatteryLevels = new BatteryData[]
                {
                    new BatteryData()
                    {
                        BatteryId = "B1",
                        BatteryLevel = (decimal)r.Next(100, 10000)/(decimal)100,
                        BatteryLevelDateTime = messageConstructionDate
                    },
                    new BatteryData()
                    {
                        BatteryId = "B2",
                        BatteryLevel = (decimal)r.Next(100, 10000)/(decimal)100,
                        BatteryLevelDateTime = messageConstructionDate
                    }
                };

                data.TankLevels = new TankData[]
                {
                    new TankData()
                    {
                        TankId = "T1",
                        TankLevel = (decimal)r.Next(100, 10000)/(decimal)100,
                        TankLevelDateTime = messageConstructionDate
                    },
                    new TankData()
                    {
                        TankId = "T2",
                        TankLevel = (decimal)r.Next(100, 10000)/(decimal)100,
                        TankLevelDateTime = messageConstructionDate
                    }
                };

                return JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            else return null;
        }

        internal static string UpdateIds(string jsonMessage, string deviceId, string moduleId)
        {
            if (string.IsNullOrEmpty(jsonMessage))
                throw new ArgumentNullException(nameof(jsonMessage));

            if (string.IsNullOrEmpty(deviceId))
                throw new ArgumentNullException(nameof(deviceId));

            JObject jobject = JObject.Parse(jsonMessage);

            if (jobject != null)
            {
                JToken jDeviceId;
                if (jobject.TryGetValue("deviceId", out jDeviceId))
                {
                    jobject["deviceId"] = JToken.FromObject(deviceId);
                }

                if (!string.IsNullOrEmpty(moduleId))
                {
                    JToken jModuleId;
                    if (jobject.TryGetValue("moduleId", out jModuleId))
                    {
                        jobject["moduleId"] = JToken.FromObject(moduleId);
                    }
                }

                return JsonConvert.SerializeObject(jobject, Formatting.Indented);
            }
            else return null;
        }
        #endregion
    }
}
