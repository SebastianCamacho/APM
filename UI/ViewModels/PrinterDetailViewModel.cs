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
        private PrinterSettings printer;

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

        partial void OnIpSegment1Changed(string value) => SyncIpAddressToModel();
        partial void OnIpSegment2Changed(string value) => SyncIpAddressToModel();
        partial void OnIpSegment3Changed(string value) => SyncIpAddressToModel();
        partial void OnIpSegment4Changed(string value) => SyncIpAddressToModel();

        private void SyncIpAddressToModel()
        {
            if (Printer != null && Printer.ConnectionType == "TCP")
            {
                Printer.IpAddress = $"{IpSegment1}.{IpSegment2}.{IpSegment3}.{IpSegment4}";
            }
        }

        [ObservableProperty]
        private List<string> printerTypes = new() { "Térmica", "Matricial" };

        public string SelectedPrinterType
        {
            get => Printer?.PrinterType ?? "Térmica";
            set
            {
                if (Printer != null && Printer.PrinterType != value)
                {
                    Printer.PrinterType = value;
                    OnPropertyChanged();
                    UpdateConnectionTypes();
                    OnPropertyChanged(nameof(IsTcpConnection));
                    OnPropertyChanged(nameof(IsUsbConnection));
                    OnPropertyChanged(nameof(ShowThermalFields));
                }
            }
        }

        [ObservableProperty]
        private List<string> connectionTypes = new() { "TCP", "USB" };

        public string SelectedConnectionType
        {
            get => Printer?.ConnectionType ?? "TCP";
            set
            {
                if (Printer != null && Printer.ConnectionType != value)
                {
                    Printer.ConnectionType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsTcpConnection));
                    OnPropertyChanged(nameof(IsUsbConnection));
                    OnPropertyChanged(nameof(ShowThermalFields));
                }
            }
        }

        public bool IsTcpConnection => SelectedConnectionType == "TCP";
        public bool IsUsbConnection => SelectedConnectionType == "USB";

        // "Matricial no se muestre ancho de papel ni los check"
        // "Térmica permita todos los campos que ya están"
        public bool ShowThermalFields => SelectedPrinterType == "Térmica";

        private void UpdateConnectionTypes()
        {
            if (SelectedPrinterType == "Térmica")
            {
                ConnectionTypes = new List<string> { "TCP" };
                SelectedConnectionType = "TCP";
            }
            else
            {
                ConnectionTypes = new List<string> { "TCP", "USB" };
            }
        }

        public List<int> AvailablePaperWidths { get; } = new List<int> { 58, 80 }; // Anchos de papel comunes

        public int SelectedPaperWidthMm
        {
            get => (Printer != null && Printer.PaperWidthMm > 0) ? Printer.PaperWidthMm : 80; // 80 por defecto si no hay valor
            set
            {
                if (Printer != null && Printer.PaperWidthMm != value)
                {
                    Printer.PaperWidthMm = value;
                    OnPropertyChanged();
                }
            }
        }

        public PrinterDetailViewModel(IPrintService printService, ILoggingService logger)
        {
            _printService = printService;
            _logger = logger;
            Printer = new PrinterSettings { CopyToPrinterIds = new System.Collections.Generic.List<string>(), Port = 9100, PaperWidthMm = 80 }; // 80 por defecto

            // Inicializar segmentos de IP
            IpSegment1 = "192";
            IpSegment2 = "168";
            IpSegment3 = "1";
            IpSegment4 = "1";
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
                Printer = new PrinterSettings { CopyToPrinterIds = new System.Collections.Generic.List<string>(), Port = 9100, ConnectionType = "TCP", PaperWidthMm = 80 }; // 80 por defecto
                IpSegment1 = "192"; IpSegment2 = "168"; IpSegment3 = "1"; IpSegment4 = "1";
            }
            OnPropertyChanged(nameof(SelectedConnectionType));
            OnPropertyChanged(nameof(IsTcpConnection));
            OnPropertyChanged(nameof(IsUsbConnection));
            // OnPrinterChanged will handle these updates
        }

        partial void OnPrinterChanged(PrinterSettings value)
        {
            if (value != null)
            {
                // Notificar a la UI que todas las propiedades proxy han cambiado
                OnPropertyChanged(nameof(SelectedPrinterType));
                OnPropertyChanged(nameof(SelectedConnectionType));
                OnPropertyChanged(nameof(IsTcpConnection));
                OnPropertyChanged(nameof(IsUsbConnection));
                OnPropertyChanged(nameof(ShowThermalFields));
                OnPropertyChanged(nameof(SelectedPaperWidthMm));

                // Asegurar que la lista de tipos de conexión sea correcta para el tipo de impresora actual
                UpdateConnectionTypes();
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
                    // Al asignar Printer, OnPrinterChanged se disparará y refrescará la UI
                    Printer = loadedPrinter;

                    // Parsear IP en segmentos para la UI
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
                }
                else
                {
                    // Si no se encontró, inicializar como nueva con el ID proporcionado
                    Printer = new PrinterSettings { PrinterId = id, CopyToPrinterIds = new System.Collections.Generic.List<string>(), Port = 9100 };
                    IpSegment1 = "192"; IpSegment2 = "168"; IpSegment3 = "1"; IpSegment4 = "1";
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al cargar la impresora '{id}': {ex.Message}", ex);
                await Shell.Current.DisplayAlertAsync("Error", "No se pudo cargar la configuración de la impresora.", "OK");
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

                // 2. Validar y combinar segmentos IP (solo si es TCP)
                if (SelectedConnectionType == "TCP")
                {
                    if (!int.TryParse(IpSegment1, out int ip1) || ip1 < 0 || ip1 > 255 ||
                        !int.TryParse(IpSegment2, out int ip2) || ip2 < 0 || ip2 > 255 ||
                        !int.TryParse(IpSegment3, out int ip3) || ip3 < 0 || ip3 > 255 ||
                        !int.TryParse(IpSegment4, out int ip4) || ip4 < 0 || ip4 > 255)
                    {
                        await Shell.Current.DisplayAlertAsync("Error", "La dirección IP debe consistir en cuatro números entre 0 y 255.", "OK");
                        IsBusy = false;
                        return;
                    }
                    // Forzar asignación final
                    SyncIpAddressToModel();

                    // 3. Validar y asignar puerto (solo si es TCP)
                    if (Printer.Port <= 0 || Printer.Port > 65535)
                    {
                        await Shell.Current.DisplayAlertAsync("Error", "El puerto debe ser un número válido entre 1 y 65535.", "OK");
                        IsBusy = false;
                        return;
                    }
                }
                else if (SelectedConnectionType == "USB")
                {
                    // Forzar que no tenga IP si es USB para evitar confusiones
                    Printer.IpAddress = null;

                    // Validar nombre de impresora local (solo si es USB)
                    if (string.IsNullOrWhiteSpace(Printer.LocalPrinterName))
                    {
                        await Shell.Current.DisplayAlertAsync("Error", "El nombre de la impresora local no puede estar vacío para conexión USB.", "OK");
                        IsBusy = false;
                        return;
                    }
                }

                // Asegurar que el tipo y conexión coincidan con la UI antes de guardar
                Printer.PrinterType = SelectedPrinterType;
                Printer.ConnectionType = SelectedConnectionType;

                // 4. Asignar y validar ancho de papel (solo si es Térmica o Matricial TCP)
                if (ShowThermalFields)
                {
                    if (SelectedPaperWidthMm <= 0)
                    {
                        await Shell.Current.DisplayAlertAsync("Error", "Debe seleccionar un ancho de papel válido.", "OK");
                        IsBusy = false;
                        return;
                    }
                    Printer.PaperWidthMm = SelectedPaperWidthMm;
                }

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
