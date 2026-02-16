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

        private string _type = "Static";
        public string Type
        {
            get => _type;
            set
            {
                if (string.IsNullOrEmpty(value) || value == _type) return;
                if (SetProperty(ref _type, value))
                {
                    OnPropertyChanged(nameof(IsTableSection));
                    OnPropertyChanged(nameof(IsDataSourceVisible));
                    UpdateElementsTableStatus();
                }
            }
        }

        private string _dataSource = string.Empty;
        public string DataSource
        {
            get => _dataSource;
            set
            {
                if (value == null || value == _dataSource) return; // Glitch check
                if (SetProperty(ref _dataSource, value))
                {
                    OnPropertyChanged(nameof(DisplayDataSourceSuggestions));
                }
            }
        }

        private string? _align;
        public string? Align
        {
            get => _align;
            set
            {
                if (string.IsNullOrEmpty(value) || value == _align) return;
                SetProperty(ref _align, value);
            }
        }

        [ObservableProperty]
        private int? order;

        [ObservableProperty]
        private bool isExpanded;

        partial void OnIsExpandedChanged(bool value)
        {
            if (value && !_isLoaded)
            {
                LoadElements();
            }
        }

        private bool _isLoaded;

        [ObservableProperty]
        private ObservableCollection<TemplateElementViewModel> elements = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DataSourceSuggestions))]
        [NotifyPropertyChangedFor(nameof(DisplayDataSourceSuggestions))]
        private string? documentType;

        private int? _selectedSizeIdx;
        public int? SelectedSizeIdx
        {
            get => _selectedSizeIdx;
            set
            {
                if ((value == null || value < 0) && _selectedSizeIdx != null) return;
                SetProperty(ref _selectedSizeIdx, value);
            }
        }

        [ObservableProperty]
        private bool isBold;



        partial void OnDocumentTypeChanged(string? value)
        {
            if (Elements != null)
            {
                foreach (var element in Elements)
                    element.DocumentType = value;
            }
        }

        private void UpdateElementsTableStatus()
        {
            if (Elements != null)
            {
                foreach (var element in Elements)
                {
                    element.IsTableSection = this.IsTableSection;
                    element.IsRepeatedSection = this.Type == "Repeated";
                }
            }
        }

        public bool IsTableSection => Type == "Table";
        public bool IsDataSourceVisible => Type == "Table" || Type == "Repeated";

        public List<string> SectionTypes { get; } = new() { "Static", "Table", "Repeated" };
        public List<string> Alignments { get; } = new() { "Ninguno", "Left", "Center", "Right" };
        public List<string> Sizes { get; } = new() { "Ninguno", "Tamaño 1", "Tamaño 2", "Tamaño 3", "Tamaño 4", "Tamaño 5", "Tamaño 6" };
        public List<string> DataSourceSuggestions => AppsielPrintManager.Core.Services.TemplateCatalogService.GetDataSourceSuggestions(DocumentType);

        public List<string> DisplayDataSourceSuggestions
        {
            get
            {
                var list = DataSourceSuggestions.ToList();
                if (!string.IsNullOrEmpty(DataSource))
                {
                    // Buscar coincidencia exacta
                    var exactMatch = list.FirstOrDefault(x => x == DataSource);
                    if (exactMatch == null)
                    {
                        // Si no hay exacta, buscar por ignorar mayúsculas y quitarla para evitar duplicados visuales
                        var caseInsensitiveMatch = list.FirstOrDefault(x => x.Equals(DataSource, StringComparison.OrdinalIgnoreCase));
                        if (caseInsensitiveMatch != null) list.Remove(caseInsensitiveMatch);

                        list.Insert(0, DataSource);
                    }
                }
                return list;
            }
        }

        public TemplateSectionViewModel(TemplateSection model, string? documentType, bool startExpanded = false)
        {
            _model = model;
            DocumentType = documentType;
            Name = model.Name ?? "Nueva Sección";
            Type = model.Type ?? "Static";
            DataSource = model.DataSource ?? string.Empty;
            Align = model.Align ?? "Ninguno";
            Order = model.Order ?? 0;
            IsExpanded = startExpanded;

            // Si es una sección nueva (sin elementos) o pedimos expandir, cargamos ya.
            if (startExpanded || _model.Elements == null || _model.Elements.Count == 0)
            {
                LoadElements();
            }

            ParseFormat(model.Format);
        }

        private void LoadElements()
        {
            if (_isLoaded || _model.Elements == null) return;

            var newElements = _model.Elements.Select(e => new TemplateElementViewModel(e, DocumentType)
            {
                IsTableSection = IsTableSection,
                IsRepeatedSection = Type == "Repeated"
            }).ToList();

            foreach (var element in newElements)
            {
                Elements.Add(element);
            }

            _isLoaded = true;
        }

        [RelayCommand]
        private void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
            if (IsExpanded && !_isLoaded)
            {
                LoadElements();
            }
        }

        private void ParseFormat(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                SelectedSizeIdx = 0; // Ninguno
                return;
            }

            var parts = format.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            IsBold = parts.Contains("Bold", StringComparer.OrdinalIgnoreCase);

            SetSizeIndex(parts, out int? sizeIdx);
            SelectedSizeIdx = sizeIdx;
        }

        private void SetSizeIndex(string[] parts, out int? sizeIdx)
        {
            bool isFontA = parts.Contains("FontA", StringComparer.OrdinalIgnoreCase);
            bool isFontB = parts.Contains("FontB", StringComparer.OrdinalIgnoreCase);
            bool isSize1 = parts.Contains("Size1", StringComparer.OrdinalIgnoreCase);
            bool isSize2 = parts.Contains("Size2", StringComparer.OrdinalIgnoreCase);
            bool isSize3 = parts.Contains("Size3", StringComparer.OrdinalIgnoreCase);

            if (isSize1 && isFontB) sizeIdx = 1;
            else if (isSize1 && (isFontA || !isFontB)) sizeIdx = 2;
            else if (isSize2 && isFontB) sizeIdx = 3;
            else if (isSize2 && (isFontA || !isFontB)) sizeIdx = 4;
            else if (isSize3 && isFontB) sizeIdx = 5;
            else if (isSize3 && (isFontA || !isFontB)) sizeIdx = 6;
            else sizeIdx = 0; // Ninguno
        }

        private string? UpdateFormat()
        {
            var formats = new List<string>();

            // Sizes mapping consistent with elements:
            // 1: FontB Size1
            // 2: FontA Size1
            // 3: FontB Size2
            // 4: FontA Size2
            // 5: FontB Size3
            // 6: FontA Size3

            switch (SelectedSizeIdx)
            {
                case 1: formats.Add("FontB Size1"); break;
                case 2: formats.Add("FontA Size1"); break;
                case 3: formats.Add("FontB Size2"); break;
                case 4: formats.Add("FontA Size2"); break;
                case 5: formats.Add("FontB Size3"); break;
                case 6: formats.Add("FontA Size3"); break;
            }

            if (IsBold) formats.Add("Bold");

            return formats.Count > 0 ? string.Join(" ", formats) : null;
        }

        [RelayCommand]
        private void AddElement()
        {
            if (!_isLoaded) LoadElements();
            IsExpanded = true;

            Elements.Add(new TemplateElementViewModel(new TemplateElement { Type = "Text", Align = "Left" }, DocumentType)
            {
                IsTableSection = IsTableSection
            });
            UpdateElementOrders();
        }

        [RelayCommand]
        private void MoveElementUp(TemplateElementViewModel element)
        {
            var index = Elements.IndexOf(element);
            if (index > 0)
            {
                var item = Elements[index];
                Elements.RemoveAt(index);
                Elements.Insert(index - 1, item);
                UpdateElementOrders();
            }
        }

        [RelayCommand]
        private void MoveElementDown(TemplateElementViewModel element)
        {
            var index = Elements.IndexOf(element);
            if (index < Elements.Count - 1)
            {
                var item = Elements[index];
                Elements.RemoveAt(index);
                Elements.Insert(index + 1, item);
                UpdateElementOrders();
            }
        }

        [RelayCommand]
        private void RemoveElement(TemplateElementViewModel element)
        {
            if (element != null)
            {
                Elements.Remove(element);
                UpdateElementOrders();
            }
        }

        private void UpdateElementOrders()
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                Elements[i].Order = i + 1;
            }
        }

        public TemplateSection ToModel()
        {
            _model.Name = Name;
            _model.Type = Type;
            _model.DataSource = string.IsNullOrEmpty(DataSource) ? null : DataSource;
            _model.Align = (Align == "Ninguno") ? null : Align;
            _model.Order = Order;
            _model.Format = UpdateFormat();

            if (_isLoaded)
            {
                _model.Elements = Elements.Select(e => e.ToModel()).ToList();
            }

            return _model;
        }
    }
}
