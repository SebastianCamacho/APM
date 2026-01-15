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
        
        [ObservableProperty]
        private string ipSegment1;
        [ObservableProperty]
        private string ipSegment2;
        [ObservableProperty]
        private string ipSegment3;
        [ObservableProperty]
        private string ipSegment4;

        [ObservableProperty]
        private int selectedPaperWidthMm;
        public List<int> AvailablePaperWidths { get; } = new List<int> { 58, 80 }; // Anchos de papel comunes

        public PrinterDetailViewModel(IPrintService printService, ILoggingService logger)
        {
            _printService = printService;
            _logger = logger;
            Printer = new PrinterSettings { CopyToPrinterIds = new System.Collections.Generic.List<string>(), Port = 9100 }; // Inicializar para evitar null y establecer puerto por defecto
            
            // Inicializar segmentos de IP y ancho de papel seleccionado
            IpSegment1 = "192";
            IpSegment2 = "168";
            IpSegment3 = "1";
            IpSegment4 = "1";
            SelectedPaperWidthMm = AvailablePaperWidths.FirstOrDefault(); // Establecer un valor por defecto o el primero disponible
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
                Printer = new PrinterSettings { CopyToPrinterIds = new System.Collections.Generic.List<string>(), Port = 9100 }; // Establecer puerto por defecto
                IpSegment1 = "192"; IpSegment2 = "168"; IpSegment3 = "1"; IpSegment4 = "1";
                SelectedPaperWidthMm = AvailablePaperWidths.FirstOrDefault(); // Establecer un valor por defecto o el primero disponible
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
                    // Parsear IP en segmentos
                    var ipParts = loadedPrinter.IpAddress?.Split('.');
                    if (ipParts?.Length == 4)
                    {
                        IpSegment1 = ipParts[0];
                        IpSegment2 = ipParts[1];
                        IpSegment3 = ipParts[2];
                        IpSegment4 = ipParts[3];
                    }
                    else
                    {
                        // Default IP if parsing fails
                        IpSegment1 = "192"; IpSegment2 = "168"; IpSegment3 = "1"; IpSegment4 = "1";
                    }

                    // Establecer el ancho de papel seleccionado de forma robusta
                    var targetPaperWidth = loadedPrinter.PaperWidthMm;
                    var itemToSelect = AvailablePaperWidths.FirstOrDefault(w => w == targetPaperWidth);

                    if (itemToSelect != default(int)) // default(int) is 0. This works as 0 is not a valid paper width in this context.
                    {
                        // Workaround: If the item to select is the first in the list,
                        // temporarily set to an invalid value and then back to force UI refresh.
                        if (itemToSelect == AvailablePaperWidths.FirstOrDefault())
                        {
                            var correctValue = itemToSelect;
                            SelectedPaperWidthMm = 0; // Establecer temporalmente a un valor inválido (asumiendo 0 no está en AvailablePaperWidths)
                            await Task.Yield(); // Permitir que la UI procese este cambio
                            SelectedPaperWidthMm = correctValue; // Establecer de nuevo al valor correcto
                        }
                        else
                        {
                            SelectedPaperWidthMm = itemToSelect; // Asignar la instancia real de la lista
                        }
                    }
                    else
                    {
                        // Si el valor cargado no está en la lista (o es 0), por defecto al primero
                        SelectedPaperWidthMm = AvailablePaperWidths.FirstOrDefault(); 
                        // También actualizar el Printer.PaperWidthMm subyacente para reflejar este valor por defecto
                        Printer.PaperWidthMm = SelectedPaperWidthMm;
                    }
                }
                else
                {
                    _logger.LogWarning($"No se encontró impresora con ID: {id}. Creando nueva.");
                    Printer = new PrinterSettings { PrinterId = id, CopyToPrinterIds = new System.Collections.Generic.List<string>(), Port = 9100 }; // Establecer puerto por defecto
                    // Default IP for new printer
                    IpSegment1 = "192"; IpSegment2 = "168"; IpSegment3 = "1"; IpSegment4 = "1";
                    SelectedPaperWidthMm = AvailablePaperWidths.FirstOrDefault(); // Establecer un valor por defecto o el primero disponible
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
                // 1. Validaciones básicas
                if (string.IsNullOrWhiteSpace(Printer.PrinterId))
                {
                    await Shell.Current.DisplayAlertAsync("Error", "El ID de la impresora no puede estar vacío.", "OK");
                    IsBusy = false;
                    return;
                }

                // 2. Validar y combinar segmentos IP
                if (!int.TryParse(IpSegment1, out int ip1) || ip1 < 0 || ip1 > 255 ||
                    !int.TryParse(IpSegment2, out int ip2) || ip2 < 0 || ip2 > 255 ||
                    !int.TryParse(IpSegment3, out int ip3) || ip3 < 0 || ip3 > 255 ||
                    !int.TryParse(IpSegment4, out int ip4) || ip4 < 0 || ip4 > 255)
                {
                    await Shell.Current.DisplayAlertAsync("Error", "La dirección IP debe consistir en cuatro números entre 0 y 255.", "OK");
                    IsBusy = false;
                    return;
                }
                Printer.IpAddress = $"{IpSegment1}.{IpSegment2}.{IpSegment3}.{IpSegment4}";

                // 3. Validar y asignar puerto
                if (Printer.Port <= 0 || Printer.Port > 65535)
                {
                    await Shell.Current.DisplayAlertAsync("Error", "El puerto debe ser un número válido entre 1 y 65535.", "OK");
                    IsBusy = false;
                    return;
                }
                
                // 4. Asignar y validar ancho de papel
                if (SelectedPaperWidthMm <= 0)
                {
                    await Shell.Current.DisplayAlertAsync("Error", "Debe seleccionar un ancho de papel válido.", "OK");
                    IsBusy = false;
                    return;
                }
                Printer.PaperWidthMm = SelectedPaperWidthMm;

                await _printService.ConfigurePrinterAsync(Printer);
                _logger.LogInfo($"Impresora '{Printer.PrinterId}' guardada exitosamente.");
                await Shell.Current.GoToAsync(".."); // Volver a la página anterior
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al guardar impresora '{Printer?.PrinterId}': {ex.Message}", ex);
                await Shell.Current.DisplayAlertAsync("Error", $"No se pudo guardar la impresora: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
