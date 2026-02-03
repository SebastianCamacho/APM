using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppsielPrintManager.Core.Models;

namespace AppsielPrintManager.Core.Interfaces
{
    public interface IScaleService
    {
        // Evento único con ScaleId para distinguir origen
        event EventHandler<ScaleDataEventArgs> OnWeightChanged;

        Task InitializeAsync();
        ScaleStatus GetStatus(string scaleId);
        List<ScaleStatusInfo> GetAllStatuses();

        // Gestión de transmisión a clientes (Listening logic)
        void StartListening(string scaleId);
        void StopListening(string scaleId);

        // Recargar configuraciones si cambian en DB/JSON
        Task ReloadScalesAsync();

        ScaleData? GetLastScaleReading(); // Added nullable return type
    }

    public class ScaleDataEventArgs : EventArgs
    {
        public string ScaleId { get; set; }
        public ScaleData Data { get; set; }

        public ScaleDataEventArgs(string scaleId, ScaleData data)
        {
            ScaleId = scaleId;
            Data = data;
        }
    }

    public enum ScaleStatus
    {
        Disconnected,
        Connected,
        Error
    }

    public class ScaleStatusInfo
    {
        public string ScaleId { get; set; }
        public ScaleStatus Status { get; set; }
        public string? ErrorMessage { get; set; } // Added for debugging
    }
}
