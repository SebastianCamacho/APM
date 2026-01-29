using CommunityToolkit.Mvvm.ComponentModel;
using AppsielPrintManager.Core.Models;
using System.Collections.Generic;
using System;
using System.Linq;

namespace UI.ViewModels
{
    public partial class TemplateElementViewModel : ObservableObject
    {
        private readonly TemplateElement _model;

        [ObservableProperty]
        private string type;

        [ObservableProperty]
        private string label;

        [ObservableProperty]
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
        private bool isItalic;

        [ObservableProperty]
        private int? widthPercentage;

        public List<string> Alignments { get; } = new() { "Left", "Center", "Right" };
        public List<string> Sizes { get; } = new() { "Tamaño 1", "Tamaño 2", "Tamaño 3", "Tamaño 4", "Tamaño 5", "Tamaño 6" };
        public List<string> ElementTypes { get; } = new() { "Text", "Line", "Barcode", "QR", "Image" };

        public TemplateElementViewModel(TemplateElement model)
        {
            _model = model;
            Type = model.Type ?? "Text";
            Label = model.Label ?? string.Empty;
            Source = model.Source ?? string.Empty;
            StaticValue = model.StaticValue ?? string.Empty;
            Align = model.Align ?? "Left";
            WidthPercentage = model.WidthPercentage;

            ParseFormat(model.Format ?? string.Empty);
        }

        private void ParseFormat(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                SelectedSizeIdx = 1; // Default
                return;
            }

            var parts = format.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            IsBold = parts.Contains("Bold", StringComparer.OrdinalIgnoreCase);
            IsItalic = parts.Contains("Italic", StringComparer.OrdinalIgnoreCase);

            // Determine size
            bool isFontA = parts.Contains("FontA", StringComparer.OrdinalIgnoreCase);
            bool isFontB = parts.Contains("FontB", StringComparer.OrdinalIgnoreCase);
            bool isSize1 = parts.Contains("Size1", StringComparer.OrdinalIgnoreCase);
            bool isSize2 = parts.Contains("Size2", StringComparer.OrdinalIgnoreCase);
            bool isSize3 = parts.Contains("Size3", StringComparer.OrdinalIgnoreCase);

            if (isSize1 && isFontB) SelectedSizeIdx = 0;
            else if (isSize1 && (isFontA || !isFontB)) SelectedSizeIdx = 1;
            else if (isSize2 && isFontB) SelectedSizeIdx = 2;
            else if (isSize2 && (isFontA || !isFontB)) SelectedSizeIdx = 3;
            else if (isSize3 && isFontB) SelectedSizeIdx = 4;
            else if (isSize3 && (isFontA || !isFontB)) SelectedSizeIdx = 5;
            else SelectedSizeIdx = 1; // Default Size 2 (FontA Size1)
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
            if (IsItalic) formats.Add("Italic");

            return string.Join(" ", formats);
        }
    }
}
