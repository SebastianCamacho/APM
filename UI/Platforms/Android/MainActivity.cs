using Android.App;
using Android.Content.PM;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace UI
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
                  Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
                  DataScheme = "apm",
                  DataHost = "update")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnDestroy()
        {
            // No llamar al base.OnDestroy() inmediatamente - permite que los fragmentos se limpien primero
            try
            {
                base.OnDestroy();
            }
            catch (ObjectDisposedException)
            {
                // Ignorar errores de disposición durante shutdown
                System.Diagnostics.Debug.WriteLine("ObjectDisposedException durante OnDestroy - ignorado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnDestroy: {ex}");
            }
        }
    }
}
