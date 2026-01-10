using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging; // Añadir este using
using System.Threading.Tasks;
using UI; // Añadir este using para LoginSuccessMessage (definido en App.xaml.cs)

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
    }
}
