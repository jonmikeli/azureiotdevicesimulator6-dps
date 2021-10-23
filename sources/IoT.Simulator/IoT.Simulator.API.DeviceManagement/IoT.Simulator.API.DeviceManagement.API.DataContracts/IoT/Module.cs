﻿using System;

namespace IoT.Simulator.API.DeviceManagement.API.DataContracts.IoT
{
    /// <summary>
    /// Module datacontract summary to be replaced
    /// </summary>
    public class Module
    {
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public string GenerationId { get; set; }
        public string ETag { get; set; }
        public string ConnectionState { get; set; }      
        public string ManagedBy { get; set; }
        public DateTime ConnectionStateUpdatedTime { get; set; }        
        public DateTime LastActivityTime { get; set; }
        public int CloudToDeviceMessageCount { get; set; }
        public string AuthenticationMechanism { get; set; }
        public string AuthenticationPrimaryKey { get; set; }
        public string AuthenticationSecondaryKey { get; set; }        
    }
}
