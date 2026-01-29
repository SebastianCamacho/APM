using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppsielPrintManager.Core.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace UI.ViewModels
{
    public partial class TemplateSectionViewModel : ObservableObject
    {
        private readonly TemplateSection _model;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTableSection))]
        private string type;

        partial void OnTypeChanged(string value) => UpdateElementsTableStatus();

        private void UpdateElementsTableStatus()
        {
            if (Elements != null)
            {
                foreach (var element in Elements)
                    element.IsTableSection = this.IsTableSection;
            }
        }

        [ObservableProperty]
        private string dataSource;

        [ObservableProperty]
        private string align;

        [ObservableProperty]
        private int? order;

        [ObservableProperty]
        private ObservableCollection<TemplateElementViewModel> elements;

        [ObservableProperty]
        private int selectedSizeIdx;

        [ObservableProperty]
        private bool isBold;



        public bool IsTableSection => Type == "Table";

        public List<string> SectionTypes { get; } = new() { "Static", "Table", "Repeated" };
        public List<string> Alignments { get; } = new() { "Left", "Center", "Right" };
        public List<string> Sizes { get; } = new() { "Tamaño 1", "Tamaño 2", "Tamaño 3", "Tamaño 4", "Tamaño 5", "Tamaño 6" };
        public List<string> DataSourceSuggestions => AppsielPrintManager.Core.Services.TemplateCatalogService.GetDataSourceSuggestions();

        public TemplateSectionViewModel(TemplateSection model)
        {
            _model = model;
            Name = model.Name ?? "Nueva Sección";
            Type = model.Type ?? "Static";
            DataSource = model.DataSource ?? string.Empty;
            Align = model.Align ?? "Left";
            Order = model.Order ?? 0;

            Elements = new ObservableCollection<TemplateElementViewModel>(
                model.Elements.Select(e => new TemplateElementViewModel(e) { IsTableSection = IsTableSection })
            );

            ParseFormat(model.Format);
        }

        private void ParseFormat(string format)
        {
            if (string.IsNullOrEmpty(format)) return;

            IsBold = format.Contains("Bold", StringComparison.OrdinalIgnoreCase);

            if (format.Contains("Size1")) SelectedSizeIdx = 0;
            else if (format.Contains("Size2")) SelectedSizeIdx = 1;
            else if (format.Contains("Size3")) SelectedSizeIdx = 2;
            else if (format.Contains("Size4")) SelectedSizeIdx = 3;
            else if (format.Contains("Size5")) SelectedSizeIdx = 4;
            else if (format.Contains("Size6")) SelectedSizeIdx = 5;
        }

        private string UpdateFormat()
        {
            var parts = new List<string>();

            // Mapeo inverso de los 6 tamaños
            string sizePart = (SelectedSizeIdx + 1) switch
            {
                1 => "FontA Size1",
                2 => "FontA Size2",
                3 => "FontA Size3",
                4 => "FontB Size1",
                5 => "FontB Size2",
                6 => "FontB Size3",
                _ => "FontA Size1"
            };
            parts.Add(sizePart);

            if (IsBold) parts.Add("Bold");

            return string.Join(" ", parts);
        }

        [RelayCommand]
        private void AddElement()
        {
            Elements.Add(new TemplateElementViewModel(new TemplateElement { Type = "Text", Align = "Left" }) { IsTableSection = IsTableSection });
        }

        [RelayCommand]
        private void RemoveElement(TemplateElementViewModel element)
        {
            if (element != null)
                Elements.Remove(element);
        }

        public TemplateSection ToModel()
        {
            _model.Name = Name;
            _model.Type = Type;
            _model.DataSource = string.IsNullOrEmpty(DataSource) ? null : DataSource;
            _model.Align = Align;
            _model.Order = Order;
            _model.Format = UpdateFormat();
            _model.Elements = Elements.Select(e => e.ToModel()).ToList();

            return _model;
        }
    }
}
