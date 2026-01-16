using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    public interface ITrayAppService
    {
        bool IsTrayAppRunning { get; }
        Task<bool> StartTrayAppAsync();
    }
}
