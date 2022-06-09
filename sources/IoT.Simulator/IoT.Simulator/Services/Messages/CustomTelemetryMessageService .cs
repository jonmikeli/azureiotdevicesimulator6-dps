using IoT.Simulator.Extensions;
using IoT.Simulator.Models;
using IoT.Simulator.Services.Business;
using IoT.Simulator.Tools;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.IO;
using System.Threading.Tasks;

namespace IoT.Simulator.Services
{
    //https://dejanstojanovic.net/aspnet/2018/december/registering-multiple-implementations-of-the-same-interface-in-aspnet-core/
    public class CustomTelemetryMessageService : ITelemetryMessageService
    {
        private ILogger _logger;
        private IGeoLocalizationService _geoLocalizationService;
        private string fileTemplatePath = @"./Messages/telemetriesBundle.v1.json";

        public CustomTelemetryMessageService(IGeoLocalizationService geoLocalizationService,  ILoggerFactory loggerFactory)
        {
            if (geoLocalizationService == null)
                throw new ArgumentNullException(nameof(geoLocalizationService));
            
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _geoLocalizationService = geoLocalizationService;
            _logger = loggerFactory.CreateLogger<CustomTelemetryMessageService>();
        }

        public async Task<string> GetMessageAsync()
        {
            string messageString = File.ReadAllText(fileTemplatePath);

            if (string.IsNullOrEmpty(messageString))
                throw new ArgumentNullException(nameof(messageString), "DATA: The message to send is empty or not found.");

            return messageString;
        }

        public async Task<string> GetMessageAsync(string deviceId, string moduleId)
        {
            string artifactId = string.IsNullOrEmpty(moduleId) ? deviceId : moduleId;

            string logPrefix = "CustomTelemetryMessageService".BuildLogPrefix();
            string messageString = await GetMessageAsync();

            if (string.IsNullOrEmpty(messageString))
                throw new ArgumentNullException(nameof(messageString), "DATA: The message to send is empty or not found.");

            _logger.LogTrace($"{logPrefix}::{artifactId}::telemetriesBundle.v1.json file loaded.");

            messageString = IoTTools.UpdateIds(messageString, deviceId, moduleId);
            _logger.LogTrace($"{logPrefix}::{artifactId}::DeviceId and moduleId updated in the message template.");

            return messageString;
        }

        public async Task<string> GetRandomizedMessageAsync(string deviceId, string moduleId)
        {
            string artifactId = string.IsNullOrEmpty(moduleId) ? deviceId : moduleId;

            string messageString = await this.GetMessageAsync(deviceId, moduleId);
            string logPrefix = "CustomTelemetryMessageService".BuildLogPrefix();

            //Randomize data           
            messageString = RandomizeCustomData(messageString);
            _logger.LogTrace($"{logPrefix}::{artifactId}::Randomized data to update template's values before sending the message.");

            return messageString;
        }

        internal string RandomizeCustomData(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage))
                throw new ArgumentNullException(nameof(jsonMessage));

            CustomTelemetryTit data = JsonConvert.DeserializeObject<CustomTelemetryTit>(jsonMessage);

            if (data != null)
            {
                Random r = new Random(DateTime.Now.Second);
                DateTime messageConstructionDate = DateTime.UtcNow;

                data.Position = _geoLocalizationService.RandomizePosition(PositionRandomType.FullRandom, RandomPositionPrecision.m);

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
    }
}
