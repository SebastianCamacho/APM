using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System.Threading.Tasks;

namespace UI.ViewModels
{
    [QueryProperty(nameof(PrinterId), "printerId")]
    public partial class PrinterDetailViewModel : ObservableObject
    {
        private readonly IPrintService _printService;
        private readonly ILoggingService _logger;

        [ObservableProperty]
        public PrinterSettings printer;

        [ObservableProperty]
        public bool isBusy;

        [ObservableProperty]
        private string printerId; // Para recibir el ID de la impresora si se está editando

        public PrinterDetailViewModel(IPrintService printService, ILoggingService logger)
        {
            _printService = printService;
            _logger = logger;
            Printer = new PrinterSettings { CopyToPrinterIds = new System.Collections.Generic.List<string>() }; // Inicializar para evitar null
        }

        partial void OnPrinterIdChanged(string oldValue, string newValue)
        {
            // Cuando el PrinterId cambia (ej. al navegar a la página), cargar la impresora
            if (!string.IsNullOrEmpty(newValue))
            {
                LoadPrinter(newValue);
            }
            else
            {
                // Si el ID es nulo o vacío, es una nueva impresora
                Printer = new PrinterSettings { CopyToPrinterIds = new System.Collections.Generic.List<string>() };
            }
        }

        private async void LoadPrinter(string id)
        {
            IsBusy = true;
            try
            {
                var loadedPrinter = await _printService.GetPrinterSettingsAsync(id);
                if (loadedPrinter != null)
                {
                    Printer = loadedPrinter;
                }
                else
                {
                    _logger.LogWarning($"No se encontró impresora con ID: {id}. Creando nueva.");
                    Printer = new PrinterSettings { PrinterId = id, CopyToPrinterIds = new System.Collections.Generic.List<string>() };
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al cargar impresora con ID {id}: {ex.Message}", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SavePrinter()
        {
            IsBusy = true;
            try
            {
                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(Printer.PrinterId))
                {
                    await Shell.Current.DisplayAlert("Error", "El ID de la impresora no puede estar vacío.", "OK");
                    IsBusy = false;
                    return;
                }
                if (string.IsNullOrWhiteSpace(Printer.IpAddress))
                {
                    await Shell.Current.DisplayAlert("Error", "La dirección IP no puede estar vacía.", "OK");
                    IsBusy = false;
                    return;
                }
                if (Printer.Port <= 0 || Printer.Port > 65535)
                {
                    await Shell.Current.DisplayAlert("Error", "El puerto debe ser un número válido entre 1 y 65535.", "OK");
                    IsBusy = false;
                    return;
                }
                if (Printer.PaperWidthMm <= 0)
                {
                    await Shell.Current.DisplayAlert("Error", "El ancho del papel debe ser un número positivo.", "OK");
                    IsBusy = false;
                    return;
                }

                await _printService.ConfigurePrinterAsync(Printer);
                _logger.LogInfo($"Impresora '{Printer.PrinterId}' guardada exitosamente.");
                await Shell.Current.GoToAsync(".."); // Volver a la página anterior
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al guardar impresora '{Printer?.PrinterId}': {ex.Message}", ex);
                await Shell.Current.DisplayAlert("Error", $"No se pudo guardar la impresora: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
