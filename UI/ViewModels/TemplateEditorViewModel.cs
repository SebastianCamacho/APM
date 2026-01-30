using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace UI.ViewModels
{
    [QueryProperty(nameof(Template), "Template")]
    public partial class TemplateEditorViewModel : ObservableObject
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly ILoggingService _logger;

        [ObservableProperty]
        private PrintTemplate template = null!;

        [ObservableProperty]
        private string templateName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<TemplateSectionViewModel> sections = new();

        [ObservableProperty]
        private bool isInitializing;

        [ObservableProperty]
        private bool isBusy;

        public TemplateEditorViewModel(ITemplateRepository templateRepository, ILoggingService logger)
        {
            _templateRepository = templateRepository;
            _logger = logger;
        }

        partial void OnTemplateChanged(PrintTemplate value)
        {
            if (value != null)
            {
                TemplateName = value.Name ?? string.Empty;
                // No llamamos a InitializeAsync aquí para evitar bloquear la transición de navegación en Android
            }
        }

        [RelayCommand]
        private async Task LoadData()
        {
            if (Template == null || Sections.Count > 0 || IsInitializing) return;
            await InitializeAsync(Template);
        }

        private async Task InitializeAsync(PrintTemplate value)
        {
            if (IsInitializing) return;

            IsInitializing = true;
            try
            {
                // Esperar a que la transición de navegación termine completamente en Android
                await Task.Delay(300);

                var mappedSections = await Task.Run(() =>
                    value.Sections.OrderBy(s => s.Order ?? 0)
                                 .Select(s => new TemplateSectionViewModel(s, value.DocumentType))
                                 .ToList()
                );

                Sections.Clear();
                // Carga incremental para no congelar la UI de Android con un solo layout pass masivo
                foreach (var section in mappedSections)
                {
                    Sections.Add(section);
                    await Task.Delay(50); // Pequeño respiro para que el ActivityIndicator siga girando
                }
            }
            finally
            {
                IsInitializing = false;
            }
        }

        [RelayCommand]
        private void AddSection()
        {
            var nextOrder = (Sections.Max(s => s.Order) ?? 0) + 1;
            Sections.Add(new TemplateSectionViewModel(new TemplateSection
            {
                Name = "Nueva Sección",
                Order = nextOrder,
                Type = "Static"
            }, Template.DocumentType));
        }

        [RelayCommand]
        private void RemoveSection(TemplateSectionViewModel section)
        {
            if (section != null)
                Sections.Remove(section);
        }

        [RelayCommand]
        private void MoveUp(TemplateSectionViewModel section)
        {
            int index = Sections.IndexOf(section);
            if (index > 0)
            {
                Sections.Move(index, index - 1);
                UpdateOrders();
            }
        }

        [RelayCommand]
        private void MoveDown(TemplateSectionViewModel section)
        {
            int index = Sections.IndexOf(section);
            if (index < Sections.Count - 1)
            {
                Sections.Move(index, index + 1);
                UpdateOrders();
            }
        }

        private void UpdateOrders()
        {
            for (int i = 0; i < Sections.Count; i++)
            {
                Sections[i].Order = i + 1;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (Template == null) return;

            IsBusy = true;
            try
            {
                Template.Name = TemplateName;
                UpdateOrders(); // Asegurar órdenes correlativos
                Template.Sections = Sections.Select(s => s.ToModel()).ToList();

                await _templateRepository.SaveTemplateAsync(Template);
                await Shell.Current.DisplayAlert("Éxito", "Plantilla guardada correctamente", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error guardando plantilla: {ex.Message}", ex);
                await Shell.Current.DisplayAlert("Error", "No se pudo guardar la plantilla", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
