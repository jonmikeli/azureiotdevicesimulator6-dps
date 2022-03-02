﻿using IoT.Simulator.Extensions;
using IoT.Simulator.Settings;
using IoT.Simulator.Settings.DPS;
using IoT.Simulator.Tools;

using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IoT.Simulator.Services
{
    public class DPSProvisioningServiceX509CA : IProvisioningService
    {
        private AppSettings _appSettings;
        private DPSSettings _dpsSettings;
        private IOptionsMonitor<DeviceSettings> _deviceSettingsDelegate;
        private readonly ILogger<DPSProvisioningServiceX509CA> _logger;

        public DPSProvisioningServiceX509CA(
            IOptions<AppSettings> appSettings,
            IOptions<DPSSettings> dpsSettings,
            IOptionsMonitor<DeviceSettings> deviceSettingsDelegate,
            ILoggerFactory loggerFactory)
        {
            if (appSettings == null)
                throw new ArgumentNullException(nameof(appSettings));

            if (deviceSettingsDelegate == null)
                throw new ArgumentNullException(nameof(deviceSettingsDelegate));

            if (dpsSettings == null)
                throw new ArgumentNullException(nameof(dpsSettings));

            if (dpsSettings.Value == null)
                throw new ArgumentNullException("dpsSettings.Value", "No device configuration has been loaded.");

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory), "No logger factory has been provided.");

            _appSettings = appSettings.Value;
            _dpsSettings = dpsSettings.Value;
            _deviceSettingsDelegate = deviceSettingsDelegate;

            _logger = loggerFactory.CreateLogger<DPSProvisioningServiceX509CA>();

            string logPrefix = "system.dps.provisioning".BuildLogPrefix();
            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Logger created.");
            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::DPS Provisioning service created.");
        }

        public async Task<string> ProvisionDevice()
        {
            string result = string.Empty;
            string logPrefix = "DPSProvisioningService.ProvisionDevice".BuildLogPrefix();

            // When registering with a symmetric key using a group enrollment, the provided key will not
            // work for a specific device, rather it must be computed based on two values: the group enrollment
            // key and the desired device Id.
            if (_dpsSettings.EnrollmentType == EnrollmentType.Group)
            {
                if (_dpsSettings.GroupEnrollment == null)
                    throw new ArgumentNullException("_dpsSettings.GroupEnrollment", "No group enrollment settings have been found.");

                if (_dpsSettings.GroupEnrollment.SecurityType == SecurityType.X509CA && _dpsSettings.GroupEnrollment.CAX509Settings != null)
                {
                    string deviceCertificateFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dpsSettings.GroupEnrollment.CAX509Settings.DeviceX509Path);

                    // X509 device leaf certificate
                    string parameterDeviceId = _deviceSettingsDelegate.CurrentValue.ArtifactId;

                    X509Certificate2 deviceLeafProvisioningCertificate = new X509Certificate2(deviceCertificateFullPath, _dpsSettings.GroupEnrollment.CAX509Settings.Password);
                    _deviceSettingsDelegate.CurrentValue.DeviceId = deviceLeafProvisioningCertificate.Subject.Remove(0, 3); //delete the 'CN='

                    if (parameterDeviceId != _deviceSettingsDelegate.CurrentValue.DeviceId)
                        _logger.LogWarning($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::The leaf certificate has been created for the device '{_deviceSettingsDelegate.CurrentValue.DeviceId}' but the provided deviceId through the settings is '{parameterDeviceId}'. Provisioning will be executed with the deviceId included in the certificate.");

                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Initializing the device provisioning client...");

                    using (var security = new SecurityProviderX509Certificate(deviceLeafProvisioningCertificate))
                    {
                        using (var transportHandler = ProvisioningTools.GetTransportHandler(_dpsSettings.GroupEnrollment.CAX509Settings.TransportType))
                        {
                            ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                                _dpsSettings.GroupEnrollment.CAX509Settings.GlobalDeviceEndpoint,
                                _dpsSettings.GroupEnrollment.CAX509Settings.IdScope,
                                security,
                                transportHandler);

                            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Initialized for registration Id {security.GetRegistrationID()}.");
                            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Registering with the device provisioning service...");

                            DeviceRegistrationResult deviceRegistrationResult = await provClient.RegisterAsync();

                            if (deviceRegistrationResult != null)
                            {
                                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Registration status: {deviceRegistrationResult.Status}.");
                                if (deviceRegistrationResult.Status != ProvisioningRegistrationStatusType.Assigned)
                                {
                                    _logger.LogWarning($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Device registration process did not assign an IoT Hub.");
                                }
                                else
                                {
                                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Device {deviceRegistrationResult.DeviceId} registered to {deviceRegistrationResult.AssignedHub}.");
                                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::Creating X509 leaf authentication for IoT Hub...");

                                    _deviceSettingsDelegate.CurrentValue.DeviceId = deviceRegistrationResult.DeviceId;

                                    //HostName =< host_name >; DeviceId =< device_id >; x509 = true
                                    result = $"HostName={deviceRegistrationResult.AssignedHub};DeviceId={deviceRegistrationResult.DeviceId};x509=true";
                                }
                            }
                            else
                                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::No provisioning result has been received.");
                        }
                    }
                }
                else
                    _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::No X509 settings have been found.");
            }

            return result;
        }

        //NOTE: in this simulator, the certificate is stored locally with no strong security.
        //In production environments, the certificate should be stored in highly secured locations (KeyVaults, HSM, certificate store, etc)
        private async Task PersistAndStoreCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            string name = certificate.Subject.Remove(0, 3);

            await File.WriteAllTextAsync($"{name}", certificate.GetRawCertDataString());
        }

        public async Task<string> AddModuleIdentityToDevice(string moduleId)
        {
            if (_appSettings.DeviceManagementServiceSettings == null)
                throw new ArgumentNullException("_appSettings.DeviceManagementServiceSettings");

            if (string.IsNullOrEmpty(_appSettings.DeviceManagementServiceSettings.BaseUrl))
                throw new ArgumentNullException("_appSettings.DeviceManagementServiceSettings.BaseUrl");

            if (string.IsNullOrEmpty(_appSettings.DeviceManagementServiceSettings.AddModulesToDeviceRoute))
                throw new ArgumentNullException("_appSettings.DeviceManagementServiceSettings.AddModulesToDeviceRoute");

            if (string.IsNullOrEmpty(moduleId))
                throw new ArgumentNullException(nameof(moduleId));

            string result = string.Empty;
            string logPrefix = "DPSProvisioningService.AddModuleIdentityToDevice".BuildLogPrefix();

            try
            {

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    if (_appSettings.DeviceManagementServiceSettings.AllowAutosignedSSLCertificates)
                    {
                        //handler.ServerCertificateCustomValidationCallback = CertificateValidation;
                        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }

                    using (var client = new HttpClient(handler))
                    {
                        handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;

                        client.BaseAddress = new Uri(_appSettings.DeviceManagementServiceSettings.BaseUrl);
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        string jsonContent = JsonConvert.SerializeObject(new
                        {
                            deviceId = _deviceSettingsDelegate.CurrentValue.DeviceId,
                            moduleId = moduleId
                        });

                        HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync(_appSettings.DeviceManagementServiceSettings.AddModulesToDeviceRoute, content);

                        if (response != null)
                        {
                            response.EnsureSuccessStatusCode();

                            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::Adding the module entity to the device...");

                            string resultContent = await response.Content.ReadAsStringAsync();

                            if (!string.IsNullOrEmpty(resultContent))
                            {
                                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::Device Management service called. Getting registered module identity keys...");

                                JObject jData = JObject.Parse(resultContent);
                                string primaryKey = jData.Value<string>("authenticationPrimaryKey");

                                _logger.LogWarning($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::Keys provided by the Device Management service.");

                                if (!string.IsNullOrEmpty(primaryKey))
                                {
                                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::Module identity added to device.");

                                    result = $"HostName={_deviceSettingsDelegate.CurrentValue.HostName};DeviceId={_deviceSettingsDelegate.CurrentValue.DeviceId};ModuleId={moduleId};SharedAccessKey={primaryKey}";
                                }
                                else
                                    _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::An error has occurred when getting the keys from the Device Management service.");

                            }
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::{ex.Message}->InnerException::{ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::{ex.Message}->InnerException::{ex.InnerException?.Message}");
            }

            return result;
        }

        //TODO: think of using different security methods for modules too
        public async Task<string> AddModuleIdentityToDeviceCAX509(string moduleId)
        {
            if (_appSettings.DeviceManagementServiceSettings == null)
                throw new ArgumentNullException("_appSettings.DeviceManagementServiceSettings");

            if (string.IsNullOrEmpty(_appSettings.DeviceManagementServiceSettings.BaseUrl))
                throw new ArgumentNullException("_appSettings.DeviceManagementServiceSettings.BaseUrl");

            if (string.IsNullOrEmpty(_appSettings.DeviceManagementServiceSettings.AddModulesToDeviceRoute))
                throw new ArgumentNullException("_appSettings.DeviceManagementServiceSettings.AddModulesToDeviceRoute");

            if (string.IsNullOrEmpty(moduleId))
                throw new ArgumentNullException(nameof(moduleId));

            string result = string.Empty;
            string logPrefix = "DPSProvisioningService.AddModuleIdentityToDevice".BuildLogPrefix();

            try
            {

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    if (_appSettings.DeviceManagementServiceSettings.AllowAutosignedSSLCertificates)
                    {
                        //handler.ServerCertificateCustomValidationCallback = CertificateValidation;
                        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }

                    using (var client = new HttpClient(handler))
                    {
                        handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;

                        client.BaseAddress = new Uri(_appSettings.DeviceManagementServiceSettings.BaseUrl);
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        string jsonContent = JsonConvert.SerializeObject(new
                        {
                            deviceId = _deviceSettingsDelegate.CurrentValue.DeviceId,
                            moduleId = moduleId
                        });

                        HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync(_appSettings.DeviceManagementServiceSettings.AddModulesToDeviceRoute, content);

                        if (response != null)
                        {
                            response.EnsureSuccessStatusCode();

                            _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::Adding the module entity to the device...");

                            string resultContent = await response.Content.ReadAsStringAsync();

                            if (!string.IsNullOrEmpty(resultContent))
                            {
                                _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::Device Management service called. Getting registered module identity certificates...");

                                JObject jData = JObject.Parse(resultContent);
                                string x509DeviceCertificate = jData.Value<string>("x509DeviceCertificate");

                                _logger.LogWarning($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::Keys provided by the Device Management service.");

                                if (!string.IsNullOrEmpty(x509DeviceCertificate))
                                {
                                    _logger.LogDebug($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::Module identity added to device.");

                                    //HostName=<host_name>;DeviceId=<device_id>;x509=true
                                    result = $"HostName={_deviceSettingsDelegate.CurrentValue.HostName};DeviceId={_deviceSettingsDelegate.CurrentValue.DeviceId};ModuleId={moduleId};x509=true";
                                }
                                else
                                    _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::An error has occurred when getting the certificates from the Device Management service.");

                            }
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::{ex.Message}->InnerException::{ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{_deviceSettingsDelegate.CurrentValue.ArtifactId}::{moduleId}::{ex.Message}->InnerException::{ex.InnerException?.Message}");
            }

            return result;
        }

        private bool CertificateValidation(HttpRequestMessage requestMessage, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
