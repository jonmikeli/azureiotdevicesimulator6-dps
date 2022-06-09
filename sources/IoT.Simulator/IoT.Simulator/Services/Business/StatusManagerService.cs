using IoT.Simulator.Models;

using Microsoft.Extensions.Logging;

using System;

namespace IoT.Simulator.Services.Business
{
    public class StatusManagerService : IStatusManagerService
    {
        Status _status;
        ILogger<StatusManagerService> _iLogger;

        public StatusManagerService(ILogger<StatusManagerService> iLogger)
        {
            if (iLogger == null)
                throw new ArgumentNullException(nameof(iLogger));

            _iLogger = iLogger;

            _status = new Status
            {
                StatusValue = "online",
                StatusDateTime = DateTime.UtcNow
            };
        }

        public Status GetStatus()
        {
            return _status;
        }

        public Status UpdateStatus(string status)
        {
            _status.StatusValue = status;
            _status.StatusDateTime = DateTime.UtcNow;
            return _status;
        }
    }
}
