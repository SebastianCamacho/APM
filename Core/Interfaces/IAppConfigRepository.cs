using AppsielPrintManager.Core.Models;
using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define la interfaz para el repositorio de configuración general de la aplicación.
    /// Encargado de almacenar, recuperar y gestionar configuraciones globales como seguridad.
    /// </summary>
    public interface IAppConfigRepository
    {
        Task<AppConfig> GetConfigAsync();
        Task SaveConfigAsync(AppConfig config);
    }
}
