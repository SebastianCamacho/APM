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
            // Permitir que la interfaz del Login se dibuje completamente en la pantalla antes de saltar los permisos.
            await Task.Delay(500);

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
                    if (Application.Current?.MainPage != null)
                    {
#pragma warning disable CS0618
                        await Application.Current.MainPage.DisplayAlert("Atención",
                            "El permiso de notificaciones es OBLIGATORIO para que el servicio de impresión pueda operar en segundo plano. La aplicación se cerrará.",
                            "Entendido");
#pragma warning restore CS0618
                    }

                    // Cerrar la app o impedir el avance
                    Application.Current?.Quit();
                    return;
                }
            }

#if ANDROID
            // Solicitar evadir la optimización de batería en Android (Asegura la vida eterna del Servicio)
            try
            {
                var context = Android.App.Application.Context;
                var pm = context.GetSystemService(Android.Content.Context.PowerService) as Android.OS.PowerManager;
                if (pm != null && !pm.IsIgnoringBatteryOptimizations(context.PackageName))
                {
                    // 1. Informar al usuario antes de sacarlo de la App
                    if (Application.Current?.MainPage != null)
                    {
#pragma warning disable CS0618
                        await Application.Current.MainPage.DisplayAlert("Batería sin restricciones",
                            "Para garantizar que el servicio de impresión siga recibiendo tickets con la pantalla apagada, es OBLIGATORIO que la app funcione sin restricciones de ahorro de energía.\n\nA continuación, se abrirá una ventana donde debes seleccionar 'Permitir' o 'Sin restricciones'.",
                            "Ir a Configurar");
#pragma warning restore CS0618
                    }

                    // 2. Abrir Intent
                    var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                    intent.SetData(Android.Net.Uri.Parse($"package:{context.PackageName}"));
                    intent.SetFlags(Android.Content.ActivityFlags.NewTask);
                    context.StartActivity(intent);
                    
                    // 3. Esperar confirmación
                    if (Application.Current?.MainPage != null)
                    {
#pragma warning disable CS0618
                        await Application.Current.MainPage.DisplayAlert("Verificando permisos",
                            "¿Ya concediste el permiso en la pantalla anterior? Pulsa este botón para continuar; si la app aún detecta restricciones, se cerrará.",
                            "Sí, ya está concedido");
#pragma warning restore CS0618
                    }
                    
                    // 4. Validar el resultado de la acción del usuario
                    if (!pm.IsIgnoringBatteryOptimizations(context.PackageName))
                    {
                        ErrorMessage = "Permiso de batería denegado.";
                        HasError = true;

                        if (Application.Current?.MainPage != null)
                        {
#pragma warning disable CS0618
                            await Application.Current.MainPage.DisplayAlert("Permiso Denegado",
                                "No concediste el acceso de batería sin restricciones. La aplicación se cerrará.",
                                "Entendido");
#pragma warning restore CS0618
                        }
                        
                        // Cerrar app si no cumplió
                        Application.Current?.Quit();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error solicitando ignorar optimización de batería: {ex.Message}");
            }

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
