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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTableSection))]
        [NotifyPropertyChangedFor(nameof(IsLine))]
        [NotifyPropertyChangedFor(nameof(IsNotLine))]
        [NotifyPropertyChangedFor(nameof(IsText))]
        [NotifyPropertyChangedFor(nameof(ShowStaticToggle))]
        [NotifyPropertyChangedFor(nameof(ShowLabelAndSource))]
        [NotifyPropertyChangedFor(nameof(ShowStaticValueInput))]
        [NotifyPropertyChangedFor(nameof(ShowTableProperties))]
        private string type;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowLabelAndSource))]
        [NotifyPropertyChangedFor(nameof(ShowStaticValueInput))]
        private bool isStatic;

        partial void OnIsStaticChanged(bool value)
        {
            if (value)
            {
                Source = string.Empty;
            }
            else
            {
                StaticValue = string.Empty;
            }
        }

        public bool IsLine => Type == "Line";
        public bool IsNotLine => !IsLine;
        public bool IsText => Type == "Text";

        public bool ShowStaticToggle => IsText;
        public bool ShowLabelAndSource => IsNotLine && !(IsText && IsStatic);
        public bool ShowStaticValueInput => IsText && IsStatic;
        public bool ShowTableProperties => IsTableSection && IsNotLine;

        [ObservableProperty]
        private string label;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplaySuggestions))]
        private string source;

        [ObservableProperty]
        private string staticValue;

        [ObservableProperty]
        private string align;

        [ObservableProperty]
        private int selectedSizeIdx = 0; // 0 to 5 for sizes 1 to 6

        [ObservableProperty]
        private bool isBold;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowTableProperties))]
        private bool isTableSection;

        [ObservableProperty]
        private int? widthPercentage;

        [ObservableProperty]
        private int selectedHeaderSizeIdx;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GlobalSuggestions))]
        [NotifyPropertyChangedFor(nameof(ItemSuggestions))]
        [NotifyPropertyChangedFor(nameof(AllSuggestions))]
        [NotifyPropertyChangedFor(nameof(DisplaySuggestions))]
        private string? documentType;

        [ObservableProperty]
        private bool isHeaderBold;

        public List<string> Alignments { get; } = new() { "Left", "Center", "Right" };
        public List<string> Sizes { get; } = new() { "Tamaño 1", "Tamaño 2", "Tamaño 3", "Tamaño 4", "Tamaño 5", "Tamaño 6" };
        public List<string> ElementTypes { get; } = new() { "Text", "Line", "Barcode", "QR", "Image" };

        public List<string> GlobalSuggestions => TemplateCatalogService.GetGlobalSourceSuggestions(DocumentType);
        public List<string> ItemSuggestions => TemplateCatalogService.GetItemSourceSuggestions(DocumentType);
        public List<string> AllSuggestions => GlobalSuggestions.Concat(ItemSuggestions).ToList();

        public List<string> DisplaySuggestions
        {
            get
            {
                var list = AllSuggestions.ToList();
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
            Align = model.Align ?? "Left";
            WidthPercentage = model.WidthPercentage;

            ParseFormat(model.Format ?? string.Empty);
            ParseHeaderFormat(model.HeaderFormat ?? string.Empty);

            IsStatic = !string.IsNullOrEmpty(StaticValue);
        }

        private void ParseFormat(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                SelectedSizeIdx = 1; // Default Size2
                return;
            }

            var parts = format.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            IsBold = parts.Contains("Bold", StringComparer.OrdinalIgnoreCase);

            SetSizeIndex(parts, out int sizeIdx);
            SelectedSizeIdx = sizeIdx;
        }

        private void ParseHeaderFormat(string format)
        {
            if (string.IsNullOrEmpty(format)) return;

            var parts = format.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            IsHeaderBold = parts.Contains("Bold", StringComparer.OrdinalIgnoreCase);

            SetSizeIndex(parts, out int sizeIdx);
            SelectedHeaderSizeIdx = sizeIdx;
        }

        private void SetSizeIndex(string[] parts, out int sizeIdx)
        {
            bool isFontA = parts.Contains("FontA", StringComparer.OrdinalIgnoreCase);
            bool isFontB = parts.Contains("FontB", StringComparer.OrdinalIgnoreCase);
            bool isSize1 = parts.Contains("Size1", StringComparer.OrdinalIgnoreCase);
            bool isSize2 = parts.Contains("Size2", StringComparer.OrdinalIgnoreCase);
            bool isSize3 = parts.Contains("Size3", StringComparer.OrdinalIgnoreCase);

            if (isSize1 && isFontB) sizeIdx = 0;
            else if (isSize1 && (isFontA || !isFontB)) sizeIdx = 1;
            else if (isSize2 && isFontB) sizeIdx = 2;
            else if (isSize2 && (isFontA || !isFontB)) sizeIdx = 3;
            else if (isSize3 && isFontB) sizeIdx = 4;
            else if (isSize3 && (isFontA || !isFontB)) sizeIdx = 5;
            else sizeIdx = 1;
        }

        public TemplateElement ToModel()
        {
            _model.Type = Type;
            _model.Label = string.IsNullOrEmpty(Label) ? null : Label;
            _model.Source = string.IsNullOrEmpty(Source) ? null : Source;
            _model.StaticValue = string.IsNullOrEmpty(StaticValue) ? null : StaticValue;
            _model.Align = Align;
            _model.WidthPercentage = WidthPercentage;
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
                case 0: formats.Add("FontB Size1"); break;
                case 1: formats.Add("FontA Size1"); break;
                case 2: formats.Add("FontB Size2"); break;
                case 3: formats.Add("FontA Size2"); break;
                case 4: formats.Add("FontB Size3"); break;
                case 5: formats.Add("FontA Size3"); break;
            }

            if (IsBold) formats.Add("Bold");

            return string.Join(" ", formats);
        }

        private string? GenerateHeaderFormat()
        {
            var formats = new List<string>();

            switch (SelectedHeaderSizeIdx)
            {
                case 0: formats.Add("FontB Size1"); break;
                case 1: formats.Add("FontA Size1"); break;
                case 2: formats.Add("FontB Size2"); break;
                case 3: formats.Add("FontA Size2"); break;
                case 4: formats.Add("FontB Size3"); break;
                case 5: formats.Add("FontA Size3"); break;
            }

            if (IsHeaderBold) formats.Add("Bold");

            return formats.Count > 0 ? string.Join(" ", formats) : null;
        }
    }
}
