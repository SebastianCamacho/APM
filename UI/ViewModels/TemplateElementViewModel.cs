using CommunityToolkit.Mvvm.ComponentModel;
using AppsielPrintManager.Core.Models;
using AppsielPrintManager.Core.Services;
using System.Collections.Generic;
using System;
using System.Linq;

namespace UI.ViewModels
{
    public partial class TemplateElementViewModel : ObservableObject
    {
        private readonly TemplateElement _model;

        private string _type = "Text";
        public string Type
        {
            get => _type;
            set
            {
                if (string.IsNullOrEmpty(value) || value == _type) return;
                if (SetProperty(ref _type, value))
                {
                    OnPropertyChanged(nameof(IsTableSection));
                    OnPropertyChanged(nameof(IsLine));
                    OnPropertyChanged(nameof(IsNotLine));
                    OnPropertyChanged(nameof(IsText));
                    OnPropertyChanged(nameof(ShowStaticToggle));
                    OnPropertyChanged(nameof(ShowLabelEntry));
                    OnPropertyChanged(nameof(ShowGenericSourcePicker));
                    OnPropertyChanged(nameof(ShowStaticValueInput));
                    OnPropertyChanged(nameof(ShowTableProperties));
                    OnPropertyChanged(nameof(IsBarcode));
                    OnPropertyChanged(nameof(IsQR));
                    OnPropertyChanged(nameof(IsImage));
                    OnPropertyChanged(nameof(ShowBarcodeProperties));
                    OnPropertyChanged(nameof(ShowBarWidth));
                    OnPropertyChanged(nameof(ShowQRProperties));
                    OnPropertyChanged(nameof(ShowTextFormatting));
                    OnPropertyChanged(nameof(ShowImageProperties));
                    OnPropertyChanged(nameof(ShowLabelAndSource));
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowLabelAndSource))]
        [NotifyPropertyChangedFor(nameof(ShowLabelEntry))]
        [NotifyPropertyChangedFor(nameof(ShowGenericSourcePicker))]
        [NotifyPropertyChangedFor(nameof(ShowStaticValueInput))]
        private bool isStatic;

        partial void OnIsStaticChanged(bool value)
        {
            // Only clear if not in a binding pulse (though hard to detect, but static/null check helps)
            if (value)
            {
                if (!string.IsNullOrEmpty(Source)) Source = string.Empty;
            }
            else
            {
                if (!string.IsNullOrEmpty(StaticValue)) StaticValue = string.Empty;
            }
        }

        public bool IsLine => Type == "Line";
        public bool IsNotLine => !IsLine;
        public bool IsText => Type == "Text";
        public bool IsBarcode => Type == "Barcode";
        public bool IsQR => Type == "QR";
        public bool IsImage => Type == "Image";

        public bool ShowStaticToggle => IsText;
        public bool ShowLabelEntry => IsText && !IsStatic;
        public bool ShowGenericSourcePicker => (IsText && !IsStatic) || IsImage;
        public bool ShowStaticValueInput => IsText && IsStatic;
        public bool ShowTableProperties => IsTableSection && IsNotLine;

        public List<int> ColumnOptions { get; } = new() { 1, 2 };

        public bool ShowBarcodeProperties => IsBarcode;
        public bool ShowBarWidth => IsBarcode;
        public bool ShowQRProperties => IsQR;
        public bool ShowSourceSelector => IsBarcode || IsQR;
        public bool ShowTextFormatting => IsText;
        public bool ShowImageProperties => IsImage;

        public bool ShowLabelAndSource => ShowLabelEntry || ShowGenericSourcePicker;

        private int _columns = 1;
        public int Columns
        {
            get => _columns;
            set
            {
                if (value <= 0 || value == _columns) return;
                if (SetProperty(ref _columns, value))
                {
                    OnPropertyChanged(nameof(ShowBarWidth));
                }
            }
        }

        [ObservableProperty]
        private string label = string.Empty;

        private string _source = string.Empty;
        public string Source
        {
            get => _source;
            set
            {
                // Accept empty only if specifically set, but ignore null glitches from Picker
                if (value == null || value == _source) return;
                if (SetProperty(ref _source, value))
                {
                    OnPropertyChanged(nameof(DisplaySuggestions));
                }
            }
        }

        [ObservableProperty]
        private string staticValue = string.Empty;

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

        private int? _selectedSizeIdx;
        public int? SelectedSizeIdx
        {
            get => _selectedSizeIdx;
            set
            {
                // Glitch check: Picker might send null or -1 during re-binding/recycling
                if ((value == null || value < 0) && _selectedSizeIdx != null) return;
                SetProperty(ref _selectedSizeIdx, value);
            }
        }

        [ObservableProperty]
        private bool isBold;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowTableProperties))]
        private bool isTableSection;

        [ObservableProperty]
        private bool isRepeatedSection;

        [ObservableProperty]
        private int? widthPercentage;

        private int? _selectedHeaderSizeIdx;
        public int? SelectedHeaderSizeIdx
        {
            get => _selectedHeaderSizeIdx;
            set
            {
                if ((value == null || value < 0) && _selectedHeaderSizeIdx != null) return;
                SetProperty(ref _selectedHeaderSizeIdx, value);
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GlobalSuggestions))]
        [NotifyPropertyChangedFor(nameof(ItemSuggestions))]
        [NotifyPropertyChangedFor(nameof(AllSuggestions))]
        [NotifyPropertyChangedFor(nameof(DisplaySuggestions))]
        private string? documentType;

        [ObservableProperty]
        private int? barWidth;

        [ObservableProperty]
        private int? height;

        [ObservableProperty]
        private int? size;

        [ObservableProperty]
        private bool isHeaderBold;

        [ObservableProperty]
        private int? order;

        public List<string> Alignments { get; } = new() { "Ninguno", "Left", "Center", "Right" };
        public List<string> Sizes { get; } = new() { "Ninguno", "Tamaño 1", "Tamaño 2", "Tamaño 3", "Tamaño 4", "Tamaño 5", "Tamaño 6" };
        public List<string> ElementTypes { get; } = new() { "Text", "Line", "Barcode", "QR", "Image" };

        public List<string> GlobalSuggestions => TemplateCatalogService.GetGlobalSourceSuggestions(DocumentType);
        public List<string> ItemSuggestions => TemplateCatalogService.GetItemSourceSuggestions(DocumentType);
        public List<string> AllSuggestions => GlobalSuggestions.Concat(ItemSuggestions).ToList();

        public List<string> DisplaySuggestions
        {
            get
            {
                var list = AllSuggestions.ToList();

                // Siempre añadir "." para permitir referencia al objeto mismo (especialmente en Repeated)
                if (!list.Contains("."))
                    list.Insert(0, ".");

                if (!string.IsNullOrEmpty(Source))
                {
                    // Buscar coincidencia exacta
                    var exactMatch = list.FirstOrDefault(x => x == Source);
                    if (exactMatch == null)
                    {
                        // Si no hay exacta, buscar por ignorar mayúsculas y quitarla para evitar duplicados visuales
                        var caseInsensitiveMatch = list.FirstOrDefault(x => x.Equals(Source, StringComparison.OrdinalIgnoreCase));
                        if (caseInsensitiveMatch != null) list.Remove(caseInsensitiveMatch);

                        list.Insert(0, Source);
                    }
                }
                return list;
            }
        }

        public TemplateElementViewModel(TemplateElement model, string? documentType)
        {
            _model = model;
            DocumentType = documentType;
            Type = model.Type ?? "Text";
            Label = model.Label ?? string.Empty;
            Source = model.Source ?? string.Empty;
            StaticValue = model.StaticValue ?? string.Empty;
            Align = model.Align ?? "Ninguno";
            WidthPercentage = model.WidthPercentage;
            Columns = model.Columns ?? 1;
            BarWidth = model.BarWidth;
            Height = model.Height;
            Size = model.Size;
            Order = model.Order;

            ParseFormat(model.Format ?? string.Empty);
            ParseHeaderFormat(model.HeaderFormat ?? string.Empty);

            IsStatic = !string.IsNullOrEmpty(StaticValue);
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

        private void ParseHeaderFormat(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                SelectedHeaderSizeIdx = 0; // Ninguno
                return;
            }

            var parts = format.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            IsHeaderBold = parts.Contains("Bold", StringComparer.OrdinalIgnoreCase);

            SetSizeIndex(parts, out int? sizeIdx);
            SelectedHeaderSizeIdx = sizeIdx;
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

        public TemplateElement ToModel()
        {
            _model.Type = Type;
            _model.Label = string.IsNullOrEmpty(Label) ? null : Label;
            _model.Source = string.IsNullOrEmpty(Source) ? null : Source;
            _model.StaticValue = string.IsNullOrEmpty(StaticValue) ? null : StaticValue;
            _model.Align = (Align == "Ninguno") ? null : Align;
            _model.WidthPercentage = WidthPercentage;
            _model.Columns = Columns;
            _model.BarWidth = BarWidth;
            _model.Height = Height;
            _model.Size = Size;
            _model.Order = Order;
            _model.Format = GenerateFormat();
            _model.HeaderFormat = GenerateHeaderFormat();

            return _model;
        }

        private string GenerateFormat()
        {
            var formats = new List<string>();

            // Sizes mapping:
            // 0: FontB Size1 -> Tamaño 1
            // 1: FontA Size1 -> Tamaño 2
            // 2: FontB Size2 -> Tamaño 3
            // 3: FontA Size2 -> Tamaño 4
            // 4: FontB Size3 -> Tamaño 5
            // 5: FontA Size3 -> Tamaño 6

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

            return string.Join(" ", formats);
        }

        private string? GenerateHeaderFormat()
        {
            var formats = new List<string>();

            switch (SelectedHeaderSizeIdx)
            {
                case 1: formats.Add("FontB Size1"); break;
                case 2: formats.Add("FontA Size1"); break;
                case 3: formats.Add("FontB Size2"); break;
                case 4: formats.Add("FontA Size2"); break;
                case 5: formats.Add("FontB Size3"); break;
                case 6: formats.Add("FontA Size3"); break;
            }

            if (IsHeaderBold) formats.Add("Bold");

            return formats.Count > 0 ? string.Join(" ", formats) : null;
        }
    }
}
