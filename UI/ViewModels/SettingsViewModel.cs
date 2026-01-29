using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using AppsielPrintManager.Core.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using UI.Views;
using System.Linq;

namespace UI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly ILoggingService _logger;

        [ObservableProperty]
        private ObservableCollection<PrintTemplate> templates = new();

        [ObservableProperty]
        private bool isBusy;

        public SettingsViewModel(ITemplateRepository templateRepository, ILoggingService logger)
        {
            _templateRepository = templateRepository;
            _logger = logger;
        }

        [RelayCommand]
        public async Task LoadTemplatesAsync()
        {
            IsBusy = true;
            try
            {
                var templateList = await _templateRepository.GetAllTemplatesAsync();

                if (templateList == null || !templateList.Any())
                {
                    // Generar plantillas por defecto si no hay ninguna
                    var defaultComanda = DefaultTemplateProvider.GetDefaultTemplate("comanda");
                    var defaultTicket = DefaultTemplateProvider.GetDefaultTemplate("ticket_venta");

                    await _templateRepository.SaveTemplateAsync(defaultComanda);
                    await _templateRepository.SaveTemplateAsync(defaultTicket);

                    templateList = await _templateRepository.GetAllTemplatesAsync();
                }

                Templates.Clear();
                foreach (var template in templateList)
                {
                    Templates.Add(template);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error cargando plantillas: {ex.Message}", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EditTemplate(PrintTemplate template)
        {
            if (template == null) return;

            // Navegar a la página de edición pasando la plantilla
            await Shell.Current.GoToAsync(nameof(TemplateEditorPage), new Dictionary<string, object>
            {
                { "Template", template }
            });
        }
    }
}
