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
        private string type;

        [ObservableProperty]
        private string dataSource;

        [ObservableProperty]
        private string align;

        [ObservableProperty]
        private int? order;

        [ObservableProperty]
        private ObservableCollection<TemplateElementViewModel> elements;

        public List<string> SectionTypes { get; } = new() { "Static", "Table", "Repeated" };
        public List<string> Alignments { get; } = new() { "Left", "Center", "Right" };

        public TemplateSectionViewModel(TemplateSection model)
        {
            _model = model;
            Name = model.Name ?? "Nueva Sección";
            Type = model.Type ?? "Static";
            DataSource = model.DataSource ?? string.Empty;
            Align = model.Align ?? "Left";
            Order = model.Order ?? 0;

            Elements = new ObservableCollection<TemplateElementViewModel>(
                model.Elements.Select(e => new TemplateElementViewModel(e))
            );
        }

        [RelayCommand]
        private void AddElement()
        {
            Elements.Add(new TemplateElementViewModel(new TemplateElement { Type = "Text", Align = "Left" }));
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
            _model.Elements = Elements.Select(e => e.ToModel()).ToList();

            return _model;
        }
    }
}
