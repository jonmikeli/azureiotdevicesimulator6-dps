using IoT.Simulator.Extensions;
using IoT.Simulator.Models;
using IoT.Simulator.Tools;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IoT.Simulator.Services
{
    //https://dejanstojanovic.net/aspnet/2018/december/registering-multiple-implementations-of-the-same-interface-in-aspnet-core/
    public class CustomInstrumentTelemetryMessageService : ITelemetryMessageService
    {
        private ILogger _logger;
        private string fileTemplatePath = @"./Messages/instrument.v1.json";

        public CustomInstrumentTelemetryMessageService(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<CustomInstrumentTelemetryMessageService>();
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

            string logPrefix = "CustomInstrumentTelemetryMessageService".BuildLogPrefix();
            string messageString = await GetMessageAsync();

            if (string.IsNullOrEmpty(messageString))
                throw new ArgumentNullException(nameof(messageString), "DATA: The message to send is empty or not found.");

            _logger.LogTrace($"{logPrefix}::{artifactId}::instrument.v1.json file loaded.");

            messageString = IoTTools.UpdateIds(messageString, deviceId, moduleId);
            _logger.LogTrace($"{logPrefix}::{artifactId}::DeviceId and moduleId updated in the message template.");

            return messageString;
        }

        public async Task<string> GetRandomizedMessageAsync(string deviceId, string moduleId)
        {
            string artifactId = string.IsNullOrEmpty(moduleId) ? deviceId : moduleId;

            string messageString = await this.GetMessageAsync(deviceId, moduleId);
            string logPrefix = "CustomInstrumentTelemetryMessageService".BuildLogPrefix();

            //Randomize data           
            messageString = RandomizeCustomData(messageString);
            _logger.LogTrace($"{logPrefix}::{artifactId}::Randomized data to update template's values before sending the message.");

            return messageString;
        }

        internal string RandomizeCustomData(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage))
                throw new ArgumentNullException(nameof(jsonMessage));

            var data = JsonConvert.DeserializeObject<JObject>(jsonMessage);

            if (data != null)
            {
                Random r = new Random(DateTime.Now.Second);
                DateTime messageConstructionDate = DateTime.UtcNow;

                data["logicalCorrelationId"] = $"simulator-integration-test-{messageConstructionDate.ToString("yyyyMMdd-HH")}";
                data["status"] = r.Next(0, 10) > 2 ? "OK" : "NOK";

                return JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            else return null;
        }
    }
}
