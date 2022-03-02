
using System.Threading.Tasks;

namespace IoT.Simulator.Services
{
    public interface IProvisioningService
    {
        Task<string> ProvisionDevice();
        Task<string> AddModuleIdentityToDevice(string moduleId);
    }
}
