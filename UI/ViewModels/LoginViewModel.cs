using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging; // Añadir este using
using System.Threading.Tasks;
using UI; // Añadir este using para LoginSuccessMessage (definido en App.xaml.cs)
using Microsoft.Maui.ApplicationModel;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace UI.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string password = "123456";

        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty]
        private bool hasError;

        private const string CorrectPassword = "123456"; // Contraseña estática por ahora

        [RelayCommand]
        private async Task Login()
        {

            HasError = false;
            if (Password == CorrectPassword)
            {
                // Enviar mensaje de login exitoso
                WeakReferenceMessenger.Default.Send(new LoginSuccessMessage());
            }
            else
            {
                ErrorMessage = "Contraseña incorrecta.";
                HasError = true;
            }
        }

        public async Task CheckPermissionsAsync()
        {
            // Solicitar permiso de notificaciones para Android 13+ antes de entrar
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                }

                if (status != PermissionStatus.Granted)
                {
                    ErrorMessage = "Permiso de notificaciones denegado.";
                    HasError = true;

                    // Notificar al usuario la obligatoriedad
                    if (Shell.Current != null)
                    {
                        await Shell.Current.DisplayAlertAsync("Atención",
                            "El permiso de notificaciones es OBLIGATORIO para que el servicio de impresión pueda operar en segundo plano. La aplicación se cerrará.",
                            "Entendido");
                    }

                    // Cerrar la app o impedir el avance
                    Application.Current?.Quit();
                    return;
                }
            }

#if ANDROID
            try
            {
                var platformService = Application.Current?.Handler?.MauiContext?.Services.GetService<AppsielPrintManager.Core.Interfaces.IPlatformService>();
                if (platformService != null && !platformService.IsBackgroundServiceRunning)
                {
                    await platformService.StartBackgroundServiceAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error iniciando servicio en segundo plano tras permisos: {ex.Message}");
            }
#endif
        }
    }
}
