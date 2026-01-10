using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq; // Necesario para OrderBy

namespace UI.ViewModels
{
    public partial class PrintersViewModel : ObservableObject
    {
        private readonly IPrintService _printService;
        private readonly ILoggingService _logger;

        [ObservableProperty]
        public ObservableCollection<PrinterSettings> printers;

        [ObservableProperty]
        public bool isBusy;

        public PrintersViewModel(IPrintService printService, ILoggingService logger)
        {
            _printService = printService;
            _logger = logger;
            Printers = new ObservableCollection<PrinterSettings>();
        }

        [RelayCommand]
        public async Task LoadPrinters()
        {
            IsBusy = true;
            try
            {
                _logger.LogInfo("Cargando configuraciones de impresoras...");
                var loadedPrinters = await _printService.GetAllPrinterSettingsAsync();
                Printers.Clear();
                foreach (var printer in loadedPrinters.OrderBy(p => p.PrinterId)) // Ordenar por PrinterId
                {
                    Printers.Add(printer);
                }
                _logger.LogInfo($"Configuraciones de impresoras cargadas. Total: {Printers.Count}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al cargar impresoras: {ex.Message}", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task AddPrinter()
        {
            _logger.LogInfo("Comando para añadir nueva impresora.");
            // Navegar a una página de detalle para añadir una nueva impresora.
            // Esto se manejará en la vista (PrintersPage.xaml.cs) con Shell.GoToAsync
            // o mediante un mensaje. Por ahora, solo logueamos.
            await Shell.Current.GoToAsync("PrinterDetailView"); // Asumiendo que esta es la ruta
        }

        [RelayCommand]
        public async Task EditPrinter(PrinterSettings printer)
        {
            if (printer == null) return;
            _logger.LogInfo($"Comando para editar impresora: {printer.PrinterId}");
            // Navegar a la página de detalle con el ID de la impresora para edición.
            await Shell.Current.GoToAsync($"PrinterDetailView?printerId={printer.PrinterId}");
        }

        [RelayCommand]
        public async Task DeletePrinter(PrinterSettings printer)
        {
            if (printer == null) return;
            _logger.LogInfo($"Comando para eliminar impresora: {printer.PrinterId}");
            
            // Confirmación antes de eliminar (ej. con DisplayActionSheet o DisplayAlert)
            bool confirm = await Shell.Current.DisplayAlert("Confirmar Eliminación", 
                                                            $"¿Está seguro de que desea eliminar la impresora '{printer.PrinterId}'?", 
                                                            "Sí", "No");
            if (confirm)
            {
                IsBusy = true;
                try
                {
                    await _printService.DeletePrinterSettingsAsync(printer.PrinterId);
                    Printers.Remove(printer);
                    _logger.LogInfo($"Impresora '{printer.PrinterId}' eliminada exitosamente.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError($"Error al eliminar impresora '{printer.PrinterId}': {ex.Message}", ex);
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }
}
