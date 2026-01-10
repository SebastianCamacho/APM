using Microsoft.Maui.Controls; 
using Microsoft.Maui.Devices; // Necesario para DeviceInfo y DevicePlatform

namespace UI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Ocultar el FlyoutItem de Básculas si no estamos en Windows
            if (DeviceInfo.Current.Platform != DevicePlatform.WinUI)
            {
                // Asegurarse de que el elemento XAML ha sido cargado
                ScalesFlyoutItem.IsVisible = false;
            }

            Routing.RegisterRoute("PrinterDetailView", typeof(Views.PrinterDetailPage));
        }
    }
}