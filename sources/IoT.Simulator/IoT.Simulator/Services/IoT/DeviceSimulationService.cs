using IoT.Simulator.Extensions;
using IoT.Simulator.Models;
using IoT.Simulator.Services.Business;
using IoT.Simulator.Settings;
using IoT.Simulator.Settings.DPS;
using IoT.Simulator.Tools;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IoT.Simulator.Services
{
    public class DeviceSimulationService : ISimulationService
    {
        private readonly ILogger<DeviceSimulationService> _logger;

        private IOptionsMonitor<DeviceSettings> _deviceSettingsDelegate;
        private DPSSettings _dpsSettings;
        private SimulationSettingsDevice _simulationSettings;
        private DeviceClient _deviceClient;
        private string _deviceId = string.Empty;
        private string _iotHub;
        private int _telemetryInterval;
        private int _fuelingTelemetryInterval;
        private bool _stopProcessing = false;

        private IEnumerable<ITelemetryMessageService> _telemetryMessagingServices;
        private IErrorMessageService _errorMessagingService;
        private ICommissioningMessageService _commissioningMessagingService;
        private IProvisioningService _provisioningService;
        private IGeoLocalizationService _geoLocalizationService;
        private IStatusManagerService _statusManagerService;
        private string _environmentName;

        public DeviceSimulationService(
            IOptionsMonitor<DeviceSettings> deviceSettingsDelegate,
            IOptions<DPSSettings> dpsSettings,
            IEnumerable<ITelemetryMessageService> telemetryMessagingServices,
            IErrorMessageService errorMessagingService,
            ICommissioningMessageService commissioningMessagingService,
            IProvisioningService provisioningService,
            IGeoLocalizationService geoLocalizationService,
            IStatusManagerService statusManagerService,
            ILoggerFactory loggerFactory)
        {
            if (deviceSettingsDelegate == null)
                throw new ArgumentNullException(nameof(deviceSettingsDelegate));

            if (deviceSettingsDelegate.CurrentValue == null)
                throw new ArgumentNullException("deviceSettingsDelegate.CurrentValue");

            if (deviceSettingsDelegate.CurrentValue.SimulationSettings == null)
                throw new ArgumentNullException("deviceSettingsDelegate.CurrentValue.SimulationSettings");

            if (dpsSettings == null)
                throw new ArgumentNullException(nameof(dpsSettings));

            if (dpsSettings.Value == null)
                throw new ArgumentNullException("dpsSettings.Value");

            if (telemetryMessagingServices == null)
                throw new ArgumentNullException(nameof(telemetryMessagingServices));

            if (errorMessagingService == null)
                throw new ArgumentNullException(nameof(errorMessagingService));

            if (commissioningMessagingService == null)
                throw new ArgumentNullException(nameof(commissioningMessagingService));

            if (provisioningService == null)
                throw new ArgumentNullException(nameof(provisioningService));

            if (geoLocalizationService == null)
                throw new ArgumentNullException(nameof(geoLocalizationService));

            if (statusManagerService == null)
                throw new ArgumentNullException(nameof(statusManagerService));

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory), "No logger factory has been provided.");

            _deviceSettingsDelegate = deviceSettingsDelegate;
            _simulationSettings = _deviceSettingsDelegate.CurrentValue.SimulationSettings;
            _dpsSettings = dpsSettings.Value;

            //_deviceId = _deviceSettingsDelegate.CurrentValue.DeviceId;
            _iotHub = _deviceSettingsDelegate.CurrentValue.HostName;

            _telemetryInterval = _simulationSettings.TelemetryFrecuency;
            _fuelingTelemetryInterval = _simulationSettings.FuelingFrecuency;

            _logger = loggerFactory.CreateLogger<DeviceSimulationService>();

            _telemetryMessagingServices = telemetryMessagingServices;
            _errorMessagingService = errorMessagingService;
            _commissioningMessagingService = commissioningMessagingService;
            _provisioningService = provisioningService;
            _geoLocalizationService = geoLocalizationService;
            _statusManagerService = statusManagerService;

            _environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT");

            string logPrefix = "system".BuildLogPrefix();
            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Logger created.");
            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Device simulator created.");
            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Enrollment type:{_dpsSettings.EnrollmentType.ToString()}.");
            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Security type:{_dpsSettings.GroupEnrollment?.SecurityType.ToString()}.");
        }

        ~DeviceSimulationService()
        {
            if (_deviceClient != null)
            {
                _deviceClient.CloseAsync();
                _deviceClient.Dispose();

                string logPrefix = "system".BuildLogPrefix();

                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Device simulator disposed.");
            }
        }

        public async Task InitiateSimulationAsync()
        {
            string logPrefix = "system".BuildLogPrefix();


            //Connectivity tests
            //Control if a connection string exists (ideally, stored in TPM/HSM or any secured location.
            //If there is no connection string, check if the DPS settings are provided.
            //If so, provision the device and persist the connection string for upcoming boots.
            if (string.IsNullOrEmpty(_deviceSettingsDelegate.CurrentValue.ConnectionString))
            {
                string connectionString = await _provisioningService.ProvisionDevice();

                if (!string.IsNullOrEmpty(connectionString))
                    _deviceSettingsDelegate.CurrentValue.ConnectionString = connectionString;
                else
                    throw new Exception($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::An issue occured during the provisioning process or the connetion string building process.");

                _logger.LogWarning($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Device connection string being persisted.");
                await ConfigurationHelpers.WriteDeviceSettings(_deviceSettingsDelegate.CurrentValue, _environmentName);
            }

            //At this stage, the connection string should be set properly and the device client should be able to communicate with the IoT Hub with no issues.
            try
            {
                IoTTools.CheckDeviceConnectionStringData(_deviceSettingsDelegate.CurrentValue.ConnectionString, _logger);

                // Connect to the IoT hub using the MQTT protocol
                if (_dpsSettings.GroupEnrollment != null)
                {
                    if (_dpsSettings.GroupEnrollment.SecurityType == SecurityType.SymmetricKey)
                        _deviceClient = DeviceClient.CreateFromConnectionString(
                            _deviceSettingsDelegate.CurrentValue.ConnectionString,
                            _dpsSettings.GroupEnrollment.SymmetricKeySettings.TransportType);
                    else if (_dpsSettings.GroupEnrollment.SecurityType == SecurityType.X509CA)
                    {
                        string deviceCertificateFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dpsSettings.GroupEnrollment.CAX509Settings.DeviceX509Path);
                        X509Certificate2 deviceLeafProvisioningCertificate = new X509Certificate2(deviceCertificateFullPath, _dpsSettings.GroupEnrollment.CAX509Settings.Password);

                        IAuthenticationMethod auth = new DeviceAuthenticationWithX509Certificate(_deviceSettingsDelegate.CurrentValue.DeviceId, deviceLeafProvisioningCertificate);

                        _deviceClient = DeviceClient.Create(_deviceSettingsDelegate.CurrentValue.HostName, auth, _dpsSettings.GroupEnrollment.CAX509Settings.TransportType);
                    }
                    else
                        _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Feature not implemented.");

                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Device client created.");

                    if (_simulationSettings.EnableTwinPropertiesDesiredChangesNotifications)
                    {
                        await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChange, null);
                        _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Twin Desired Properties update callback handler registered.");
                    }

                    //Configuration
                    if (_simulationSettings.EnableC2DDirectMethods)
                        //Register C2D Direct methods handlers            
                        await RegisterC2DDirectMethodsHandlersAsync();

                    if (_simulationSettings.EnableC2DMessages)
                        //Start receiving C2D messages
                        ReceiveC2DMessagesAsync();

                    //Messages
                    if (_simulationSettings.EnableLatencyTests)
                        SendDeviceToCloudLatencyTestAsync(_deviceSettingsDelegate.CurrentValue.DeviceId, _simulationSettings.LatencyTestsFrecuency);

                    if (_simulationSettings.EnableTelemetryMessages)
                        SendDeviceToCloudMessagesTelemetryAsync(_deviceSettingsDelegate.CurrentValue.DeviceId); //interval is a global variable changed by processes

                    if (_simulationSettings.EnableFuelingMessages)
                        SendDeviceToCloudMessagesFuelingAsync(_deviceSettingsDelegate.CurrentValue.DeviceId); //interval is a global variable changed by processes

                    if (_simulationSettings.EnableErrorMessages)
                        SendDeviceToCloudErrorAsync(_deviceSettingsDelegate.CurrentValue.DeviceId, _simulationSettings.ErrorFrecuency);

                    if (_simulationSettings.EnableCommissioningMessages)
                        SendDeviceToCloudCommissioningAsync(_deviceSettingsDelegate.CurrentValue.DeviceId, _simulationSettings.CommissioningFrecuency);

                    if (_simulationSettings.EnableReadingTwinProperties)
                    {
                        //Twins
                        _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::INITIALIZATION::Retrieving twin.");
                        Twin twin = await _deviceClient.GetTwinAsync();

                        if (twin != null)
                            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::INITIALIZATION::Device twin: {JsonConvert.SerializeObject(twin, Formatting.Indented)}.");
                        else
                            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::INITIALIZATION::No device twin.");
                    }

                    if (_simulationSettings.EnableFileUpload)
                    {
                        throw new NotImplementedException("File upload feature has not been implemented yet.");
                    }

                    _deviceClient.SetConnectionStatusChangesHandler(new ConnectionStatusChangesHandler(ConnectionStatusChanged));
                }
                else
                    _logger.LogWarning($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::No enrollment group has been found.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::ERROR::InitiateSimulationAsync:{ex.Message}.");
            }
        }

        private void ConnectionStatusChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            string logPrefix = "system".BuildLogPrefix();

            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Connection status changed-New status:{status.ToString()}-Reason:{reason.ToString()}.");
        }

        #region D2C                

        // Async method to send simulated telemetry
        internal async Task SendDeviceToCloudMessagesTelemetryAsync(string deviceId)
        {
            int counter = 0;
            string logPrefix = "SendDeviceToCloudMessagesTelemetryAsync".BuildLogPrefix();

            string messageString = string.Empty;

            var telemetryMessagingService = _telemetryMessagingServices.SingleOrDefault(t => t.GetType() == typeof(CustomTelemetryMessageService));

            if (telemetryMessagingService == null)
                throw new Exception($"No telemetry messaging service has been found.");

            Status status = null;
            using (_logger.BeginScope($"{logPrefix}::{DateTime.Now}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::MEASURED DATA"))
            {
                while (true)
                {
                    status = _statusManagerService.GetStatus();
                    if (status != null && !status.StatusValue.Contains("fueling"))
                    {
                        //Randomize data
                        messageString = await telemetryMessagingService.GetRandomizedMessageAsync(deviceId, string.Empty);

                        var message = new Message(Encoding.UTF8.GetBytes(messageString));
                        message.Properties.Add("messageType", "telemetry");
                        message.Properties.Add("deviceType", "truck");

                        // Add a custom application property to the message.
                        // An IoT hub can filter on these properties without access to the message body.
                        //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");
                        message.ContentType = "application/json";
                        message.ContentEncoding = "utf-8";

                        // Send the tlemetry message
                        await _deviceClient.SendEventAsync(message);
                        counter++;

                        _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Sent message: {messageString}.");
                        _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::COUNTER: {counter}.");

                        if (_stopProcessing)
                        {
                            _logger.LogDebug($"{logPrefix}::STOP PROCESSING.");
                            break;
                        }

                        await Task.Delay(_telemetryInterval * 1000);
                    }
                }
            }
        }

        internal async Task SendDeviceToCloudMessagesFuelingAsync(string deviceId)
        {
            int counter = 0;
            string logPrefix = "SendDeviceToCloudMessagesFuelingAsync".BuildLogPrefix();

            string messageString = string.Empty;

            var fuelingMessagingService = _telemetryMessagingServices.SingleOrDefault(t => t.GetType() == typeof(CustomFuelingTelemetryMessageService));
            if (fuelingMessagingService == null)
                throw new Exception($"No fueling telemetry messaging service has been found.");

            Status status = null;
            using (_logger.BeginScope($"{logPrefix}::{DateTime.Now}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::MEASURED DATA"))
            {
                while (true)
                {
                    status = _statusManagerService.GetStatus();
                    if (status != null && status.StatusValue.Contains("fueling"))
                    {
                        //Randomize data
                        messageString = await fuelingMessagingService.GetRandomizedMessageAsync(deviceId, string.Empty);

                        var message = new Message(Encoding.UTF8.GetBytes(messageString));
                        message.Properties.Add("messageType", "fuelingsimple");
                        message.Properties.Add("deviceType", "truck");

                        // Add a custom application property to the message.
                        // An IoT hub can filter on these properties without access to the message body.
                        //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");
                        message.ContentType = "application/json";
                        message.ContentEncoding = "utf-8";

                        // Send the tlemetry message
                        await _deviceClient.SendEventAsync(message);
                        counter++;

                        _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Sent message: {messageString}.");
                        _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::COUNTER: {counter}.");

                        if (_stopProcessing)
                        {
                            _logger.LogDebug($"{logPrefix}::STOP PROCESSING.");
                            break;
                        }

                        await Task.Delay(_fuelingTelemetryInterval * 1000);
                    }
                }
            }
        }

        internal async Task SendDeviceToCloudErrorAsync(string deviceId, int interval)
        {
            int counter = 0;
            string messageString = string.Empty;
            string logPrefix = "error".BuildLogPrefix();

            using (_logger.BeginScope($"{logPrefix}::{DateTime.Now}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::ERROR MESSAGE (SENT BY THE DEVICE)."))
            {
                while (true)
                {
                    messageString = await _errorMessagingService.GetRandomizedMessageAsync(deviceId, string.Empty);

                    var message = new Message(Encoding.ASCII.GetBytes(messageString));
                    message.Properties.Add("messagetype", "error");

                    // Add a custom application property to the message.
                    // An IoT hub can filter on these properties without access to the message body.
                    message.ContentType = "application/json";
                    message.ContentEncoding = "utf-8";

                    // Send the tlemetry message
                    await _deviceClient.SendEventAsync(message);
                    counter++;

                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Sent message: {messageString}.");
                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::COUNTER: {counter}.");

                    if (_stopProcessing)
                    {
                        _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::STOP PROCESSING.");
                        break;
                    }

                    await Task.Delay(interval * 1000);
                }
            }
        }

        //Commissioning messages
        internal async Task SendDeviceToCloudCommissioningAsync(string deviceId, int interval)
        {
            int counter = 0;
            string messageString = string.Empty;
            string logPrefix = "commissioning".BuildLogPrefix();

            using (_logger.BeginScope($"{logPrefix}::{DateTime.Now}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::COMMISSIONING MESSAGE"))
            {
                while (true)
                {
                    messageString = await _commissioningMessagingService.GetRandomizedMessageAsync(deviceId, string.Empty);

                    var message = new Message(Encoding.ASCII.GetBytes(messageString));
                    message.Properties.Add("messagetype", "commissioning");

                    // Add a custom application property to the message.
                    // An IoT hub can filter on these properties without access to the message body.
                    message.ContentType = "application/json";
                    message.ContentEncoding = "utf-8";

                    // Send the tlemetry message
                    await _deviceClient.SendEventAsync(message);
                    counter++;

                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Sent message: {messageString}.");
                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::COUNTER: {counter}.");

                    if (_stopProcessing)
                    {
                        _logger.LogDebug($"{logPrefix}::STOP PROCESSING.");
                        break;
                    }

                    await Task.Delay(interval * 1000);
                }
            }
        }

        internal async Task SendDeviceToCloudSingleCommissioningAsync(string deviceId, CommissioningRequest commissioningRequest)
        {
            if (commissioningRequest == null)
                throw new ArgumentNullException(nameof(commissioningRequest));

            string messageString = JsonConvert.SerializeObject(commissioningRequest);
            string logPrefix = "commissioning".BuildLogPrefix();

            using (_logger.BeginScope($"{logPrefix}::{DateTime.Now}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::SINGLE COMMISSIONING MESSAGE"))
            {                                
                var message = new Message(Encoding.UTF8.GetBytes(messageString));
                message.Properties.Add("messagetype", "commissioning");

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";

                // Send the tlemetry message
                await _deviceClient.SendEventAsync(message);

                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Sent message: {messageString}.");
            }
        }

        //Latency tests
        internal async Task SendDeviceToCloudLatencyTestAsync(string deviceId, int interval)
        {
            string logPrefix = "latency".BuildLogPrefix();

            while (true)
            {

                var data = new
                {
                    deviceId = deviceId,
                    messageType = "latency",
                    startTimestamp = DateTime.UtcNow.TimeStamp()
                };


                var messageString = JsonConvert.SerializeObject(data);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                message.Properties.Add("messagetype", "latency");
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";

                // Send the tlemetry message
                await _deviceClient.SendEventAsync(message);
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::LATENCY TEST::message sent:{messageString}.");

                if (interval <= 0)
                    break;

                if (_stopProcessing)
                {
                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::LATENCY TEST::STOP PROCESSING.");
                    break;
                }

                await Task.Delay(interval * 1000);
            }

        }

        //https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-file-upload
        private async Task FileUploadAsync<T>(string iotHub, string deviceId, List<T> dataToUpload)
        {
            try
            {
                if (dataToUpload == null)
                    throw new ArgumentNullException("dataToUpload");

                string sasUriEndPoint = $@"https://{iotHub}/devices/{deviceId}/files"; //1
                string endUploadEndpoint = $@"https://{iotHub}/devices/{deviceId}/files/notifications"; //2

                string localFileName = $"{DateTime.UtcNow.TimeStamp()}-{deviceId}.json";


                File.WriteAllText(localFileName, JsonConvert.SerializeObject(dataToUpload));

                using (var sourceData = new FileStream(localFileName, FileMode.Open))
                {
                    await _deviceClient.UploadToBlobAsync(localFileName, sourceData);
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

        }
        #endregion

        #region C2D
        #region Messages
        private async Task ReceiveC2DMessagesAsync()
        {
            string logPrefix = "c2dmessages".BuildLogPrefix();

            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Device listening to cloud to device messages.");

            Message receivedMessage = null;
            while (true)
            {
                receivedMessage = await _deviceClient.ReceiveAsync();

                if (receivedMessage == null) continue;

                await _deviceClient.CompleteAsync(receivedMessage);
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Received message: {Encoding.ASCII.GetString(receivedMessage.GetBytes())}.");

                receivedMessage = null;
            }
        }
        #endregion

        #region Direct Methods
        private async Task RegisterC2DDirectMethodsHandlersAsync()
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            try
            {
                // Create a handler for the direct method call

                await _deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD SetTelemetryInterval registered.");

                await _deviceClient.SetMethodHandlerAsync("SetFuelingTelemetryInterval", SetFuelingTelemetryInterval, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD SetFuelingTelemetryInterval registered.");

                await _deviceClient.SetMethodHandlerAsync("LatencyTestCallback", LatencyTestCallback, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD LatencyTestCallback registered.");

                await _deviceClient.SetMethodHandlerAsync("SendLatencyTest", SendLatencyTest, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD SendLatencyTest registered.");

                await _deviceClient.SetMethodHandlerAsync("Reboot", Reboot, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD Reboot registered.");

                await _deviceClient.SetMethodHandlerAsync("OnOff", StartOrStop, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD OnOff registered.");

                await _deviceClient.SetMethodHandlerAsync("ReadTwins", ReadTwinsAsync, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD ReadTwins registered.");

                await _deviceClient.SetMethodHandlerAsync("SetInitialPosition", SetInitialPosition, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD SetInitialPosition registered.");

                await _deviceClient.SetMethodHandlerAsync("SetStatus", SetStatus, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD SetStatus registered.");

                await _deviceClient.SetMethodHandlerAsync("NewCommissioningRequest", NewCommissioningRequest, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD NewCommissioningRequest registered.");

                await _deviceClient.SetMethodHandlerAsync("GenericJToken", GenericJToken, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD GenericJToken registered.");

                await _deviceClient.SetMethodHandlerAsync("Generic", Generic, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD Generic registered.");

                await _deviceClient.SetMethodDefaultHandlerAsync(DefaultC2DMethodHandler, null);
                _logger.LogTrace($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DIRECT METHOD Default handler registered.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::ERROR::RegisterC2DDirectMethodsHandlersAsync:{ex.Message}.");
            }
        }

        // Handle the direct method call
        //https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-control-device-dotnet
        private Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            var data = Encoding.UTF8.GetString(methodRequest.Data);

            // Check the payload is a single integer value
            if (Int32.TryParse(data, out _telemetryInterval))
            {
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Telemetry interval set to {_telemetryInterval} seconds.");

                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
        }

        private Task<MethodResponse> SetFuelingTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            var data = Encoding.UTF8.GetString(methodRequest.Data);

            // Check the payload is a single integer value
            if (Int32.TryParse(data, out _fuelingTelemetryInterval))
            {
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Fueling telemetry interval set to {_fuelingTelemetryInterval} seconds.");

                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
        }

        private Task<MethodResponse> GenericJToken(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            var data = Encoding.UTF8.GetString(methodRequest.Data);
            var content = JToken.FromObject(data);

            if (content != null)
            {
                _logger.LogDebug($"{logPrefix}::Generic call received: {JsonConvert.SerializeObject(content)}.");

                // Acknowledge the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowledge the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }

        private Task<MethodResponse> Generic(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            var data = Encoding.UTF8.GetString(methodRequest.Data);

            _logger.LogDebug($"{logPrefix}::Generic call received: {data}.");

            // Acknowledge the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private Task<MethodResponse> LatencyTestCallback(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            var callbackTimestamp = DateTime.UtcNow.TimeStamp();
            var data = Encoding.UTF8.GetString(methodRequest.Data);
            long initialtimestamp = 0;

            // Check the payload is a single integer value
            if (Int64.TryParse(data, out initialtimestamp))
            {
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Received data: {data}.");
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Latency test callback::latency: {callbackTimestamp - initialtimestamp} s.");
            }

            // Acknowledge the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\",\"latency\":" + (callbackTimestamp - initialtimestamp).ToString() + "}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private Task<MethodResponse> SendLatencyTest(MethodRequest methodRequest, object userContext)
        {
            SendDeviceToCloudLatencyTestAsync(_deviceId, 0);

            // Acknowledge the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private Task<MethodResponse> Reboot(MethodRequest methodRequest, object userContext)
        {
            // In a production device, you would trigger a reboot scheduled to start after this method returns.
            // For this sample, we simulate the reboot by writing to the console and updating the reported properties 
            RebootOrchestration();

            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private async Task RebootOrchestration()
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            try
            {
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Reboot order received.");
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Stoping processes...");

                _stopProcessing = true;


                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Processes stopped.");
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Rebooting...");

                // Update device twin with reboot time. 
                TwinCollection reportedProperties, reboot, lastReboot, rebootStatus;
                lastReboot = new TwinCollection();
                reboot = new TwinCollection();
                reportedProperties = new TwinCollection();
                rebootStatus = new TwinCollection();

                lastReboot["lastReboot"] = DateTime.Now;
                reboot["reboot"] = lastReboot;
                reboot["rebootStatus"] = "rebooting";
                reportedProperties["iothubDM"] = reboot;

                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

                await Task.Delay(10000);

                // Update device twin with reboot time. 
                reboot["rebootStatus"] = "online";
                reportedProperties["iothubDM"] = reboot;

                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Reboot over and system runing again.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::ERROR::RebootOrchestration:{ex.Message}.");
            }
            finally
            {
                _stopProcessing = false;
            }
        }

        private Task<MethodResponse> StartOrStop(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::StartOrStop command has been received and planified.");

            if (methodRequest.Data == null)
                throw new ArgumentNullException("methodRequest.Data");

            JObject jData = JsonConvert.DeserializeObject<JObject>(methodRequest.DataAsJson);

            //Send feedback
            var response = new
            {
                result = $"Executed direct method:{methodRequest.Name}.",
                payload = methodRequest.DataAsJson
            };

            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response, Formatting.Indented)), 200));
        }

        private Task<MethodResponse> ReadTwinsAsync(MethodRequest methodRequest, object userContext)
        {
            ReadTwins();

            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private Task<MethodResponse> SetInitialPosition(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();
            string result = string.Empty;
            int status = 200;

            try
            {
                var data = Encoding.UTF8.GetString(methodRequest.Data);

                IoT.Simulator.Models.Position position = JsonConvert.DeserializeObject<IoT.Simulator.Models.Position>(data);

                if (position != null)
                {
                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Initial position set to {data}.");

                    _geoLocalizationService.UpdateInitialPosution(new Models.Position
                    {
                        Latitude = position.Latitude,
                        Longitude = position.Longitude,
                        PositionDateTime = DateTime.UtcNow
                    });

                    // Acknowlege the direct method call with a 200 success message
                    result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                }
                else
                {
                    // Acknowlege the direct method call with a 400 error message
                    result = "{\"result\":\"Invalid parameter\"}";
                    status = 400;
                }
            }
            catch (Exception ex)
            {
                status = 400;
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{ex.Message}.");
            }

            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), status));
        }

        private async Task<MethodResponse> SetStatus(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();
            string result = string.Empty;
            int status = 200;

            try
            {
                var statusValue = Encoding.UTF8.GetString(methodRequest.Data);

                if (!string.IsNullOrEmpty(statusValue))
                {
                    statusValue = statusValue.Replace("\"", String.Empty);

                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Set status {statusValue}.");

                    _statusManagerService.UpdateStatus(statusValue);

                    await GenericTwinReportedUpdateAsync(_deviceId, string.Empty, "status", statusValue);

                    // Acknowlege the direct method call with a 200 success message
                    result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                }
                else
                {
                    // Acknowlege the direct method call with a 400 error message
                    result = "{\"result\":\"Invalid parameter\"}";
                    status = 400;
                }
            }
            catch (Exception ex)
            {
                status = 400;
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{ex.Message}.");
            }

            return new MethodResponse(Encoding.UTF8.GetBytes(result), status);
        }

        private async Task<MethodResponse> NewCommissioningRequest(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "NewCommissioningRequest".BuildLogPrefix();
            string result = string.Empty;
            int status = 200;

            try
            {
                var requestContent = Encoding.UTF8.GetString(methodRequest.Data);

                if (string.IsNullOrEmpty(requestContent))
                    throw new ArgumentNullException(nameof(requestContent));
                
                var commissioningRequest = JsonConvert.DeserializeObject<CommissioningRequest>(requestContent);

                if (commissioningRequest != null)
                {
                    await SendDeviceToCloudSingleCommissioningAsync(_deviceSettingsDelegate.CurrentValue.ArtifactId, commissioningRequest);                                       

                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Commissioning request sent {requestContent}.");

                    // Acknowlege the direct method call with a 200 success message
                    result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                }
                else
                {
                    // Acknowlege the direct method call with a 400 error message
                    result = "{\"result\":\"Invalid parameter\"}";
                    status = 400;
                }
            }
            catch (Exception ex)
            {
                status = 400;
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{ex.Message}.");
            }

            return new MethodResponse(Encoding.UTF8.GetBytes(result), status);
        }

        private Task<MethodResponse> DefaultC2DMethodHandler(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            _logger.LogWarning($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::WARNING::{methodRequest.Name} has been called but there is no registered specific method handler.");

            string message = $"Request direct method: {methodRequest.Name} but no specifif direct method handler.";

            //Send feedback
            var response = new
            {
                result = message,
                payload = methodRequest.Data != null ? methodRequest.DataAsJson : string.Empty
            };

            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response, Formatting.Indented)), 200));
        }
        #endregion
        #endregion

        #region Twins
        //https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-twin-getstarted
        internal async Task ReportConnectivityAsync()
        {
            string logPrefix = "c2dtwins".BuildLogPrefix();

            try
            {
                _logger.LogDebug($"{logPrefix}::Sending connectivity data as reported property.");

                TwinCollection reportedProperties, connectivity;
                reportedProperties = new TwinCollection();
                connectivity = new TwinCollection();
                connectivity["type"] = "cellular";
                connectivity["signalPower"] = "low";
                reportedProperties["connectivity"] = connectivity;
                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::ERROR::ReportConnectivityAsync:{ex.Message}.");
            }
        }

        internal async Task GenericTwinReportedUpdateAsync(string deviceId, string sensorId, string propertyName, dynamic value)
        {
            string logPrefix = "c2dtwins".BuildLogPrefix();

            try
            {
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Sending generic reported property update:: {propertyName}-{value}.");

                TwinCollection reportedProperties, configuration;
                reportedProperties = new TwinCollection();
                reportedProperties[propertyName] = value;

                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::ERROR::GenericTwinReportedUpdateAsync:{ex.Message}.");
            }
        }

        internal async Task ReportFirwmareUpdateAsync(string firmwareVersion, string firmwareUrl)
        {
            string logPrefix = "c2dtwins".BuildLogPrefix();

            try
            {
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Sending firmware update notification.");

                TwinCollection reportedProperties;
                reportedProperties = new TwinCollection();

                reportedProperties["newFirmwareVersion"] = firmwareVersion;
                reportedProperties["Ur"] = firmwareUrl;
                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::ERROR::ReportFirwmareUpdateAsync:{ex.Message}.");
            }
        }

        internal async Task ReadTwins()
        {
            string logPrefix = "c2dtwins".BuildLogPrefix();

            //Twins
            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::TWINS::Reading...");

            Twin twin = await _deviceClient.GetTwinAsync();

            if (twin != null)
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::TWINS:: {JsonConvert.SerializeObject(twin, Formatting.Indented)}.");
            else
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::TWINS:: No twins available.");
        }

        private async Task OnDesiredPropertyChange(TwinCollection desiredproperties, object usercontext)
        {
            string logPrefix = "c2dtwins".BuildLogPrefix();

            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::TWINS-PROPERTIES-DESIRED properties changes request notification.");

            if (desiredproperties != null)
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::TWINS-PROPERTIES-DESIRED::{JsonConvert.SerializeObject(desiredproperties, Formatting.Indented)}");
            else
                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::TWINS-PROPERTIES-DESIRED properties change is emtpy.");

        }
        #endregion
    }
}
