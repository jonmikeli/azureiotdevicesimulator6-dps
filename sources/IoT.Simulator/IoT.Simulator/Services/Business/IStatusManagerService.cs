using IoT.Simulator.Models;

namespace IoT.Simulator.Services.Business
{
    public interface IStatusManagerService
    {
        public Status GetStatus();

        public Status UpdateStatus(string status);
    }
}
