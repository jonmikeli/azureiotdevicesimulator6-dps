﻿using CommandLine;

using Microsoft.Azure.Devices.Client;

namespace IoT.Simulator.Settings.DPS
{
    /// <summary>
    /// Parameters for the application
    /// </summary>
    internal class DPSCommandParametersBase
    {
        [Option(
            's',
            "IdScope",
            Required = true,
            HelpText = "The Id Scope of the DPS instance")]
        public string IdScope { get; set; }

        //[Option(
        //    'i',
        //    "Id",
        //    Required = false,
        //    HelpText = "The registration Id when using individual enrollment, or the desired device Id when using group enrollment.")]
        //public string Id { get; set; }       

        [Option(
            'e',
            "EnrollmentType",
            Default = EnrollmentType.Group,
            Required = true,
            HelpText = "The type of enrollment: Individual or Group")]
        public EnrollmentType EnrollmentType { get; set; }

        [Option(
            'g',
            "GlobalDeviceEndpoint",
            Required = true,
            Default = "global.azure-devices-provisioning.net",
            HelpText = "The global endpoint for devices to connect to.")]
        public string GlobalDeviceEndpoint { get; set; }

        [Option(
            't',
            "TransportType",
            Default = TransportType.Mqtt,
            Required = true,
            HelpText = "The transport to use to communicate with the device provisioning instance. Possible values include Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, Amqp_Tcp_only, and Http1.")]
        public TransportType TransportType { get; set; }

        //[Option(
        //    'u',
        //    "SecurityType",
        //    Default = SecurityType.SymmetricKey,
        //    Required = true,
        //    HelpText = "The transport to use to communicate with the device provisioning instance. Possible values include Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, Amqp_Tcp_only, and Http1.")]
        //public SecurityType SecurityType { get; set; }
    }
}
