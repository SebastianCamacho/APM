using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using AppsielPrintManager.Core.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using UI.Views;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace UI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly IAppConfigRepository _appConfigRepository;
        private readonly ILoggingService _logger;

        [ObservableProperty]
        private ObservableCollection<PrintTemplate> templates = new();

        [ObservableProperty]
        private bool isBusy;

        // Propiedades para Cambio de Contraseña
        [ObservableProperty]
        private string currentPassword = string.Empty;

        [ObservableProperty]
        private string newPassword = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private string passwordErrorMessage = string.Empty;

        [ObservableProperty]
        private bool hasPasswordError;

        [ObservableProperty]
        private bool isPasswordHidden = true;

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordHidden = !IsPasswordHidden;
        }

        public SettingsViewModel(ITemplateRepository templateRepository, IAppConfigRepository appConfigRepository, ILoggingService logger)
        {
            _templateRepository = templateRepository;
            _appConfigRepository = appConfigRepository;
            _logger = logger;
        }

        [RelayCommand]
        public async Task LoadTemplatesAsync()
        {
            IsBusy = true;
            try
            {
                // Asegurar que siempre existan las 4 plantillas base (sin sobreescribir las existentes)
                await _templateRepository.EnsureDefaultTemplatesAsync();

                var templateList = await _templateRepository.GetAllTemplatesAsync();

                Templates.Clear();
                if (templateList != null)
                {
                    foreach (var template in templateList)
                    {
                        Templates.Add(template);
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error cargando plantillas: {ex.Message}", ex, "SettingsViewModel");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EditTemplate(PrintTemplate template)
        {
            if (template == null) return;

            // Navegar a la página de edición pasando la plantilla
            await Shell.Current.GoToAsync(nameof(TemplateEditorPage), new Dictionary<string, object>
            {
                { "Template", template }
            });
        }

        [RelayCommand]
        private async Task ChangePasswordAsync()
        {
            HasPasswordError = false;
            PasswordErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ShowPasswordError("Todos los campos son obligatorios.");
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ShowPasswordError("Las nuevas contraseñas no coinciden.");
                return;
            }

            // Validar política de contraseña (solo a-z, A-Z, 0-9, *, -, ., /)
            if (!Regex.IsMatch(NewPassword, @"^[a-zA-Z0-9*\-./]+$"))
            {
                ShowPasswordError("La contraseña solo puede contener letras, números y los caracteres * - . /");
                return;
            }

            var config = await _appConfigRepository.GetConfigAsync();
            string currentInputHash = ComputeSha256Hash(CurrentPassword);

            if (currentInputHash != config.AdminPasswordHash)
            {
                ShowPasswordError("La contraseña actual es incorrecta.");
                return;
            }

            // Cambiar la contraseña validada
            config.AdminPasswordHash = ComputeSha256Hash(NewPassword);
            await _appConfigRepository.SaveConfigAsync(config);

            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;

            if (Application.Current?.MainPage != null)
            {
#pragma warning disable CS0618
                await Application.Current.MainPage.DisplayAlert("Éxito", "Contraseña cambiada correctamente.", "OK");
#pragma warning restore CS0618
            }
        }

        private void ShowPasswordError(string message)
        {
            PasswordErrorMessage = message;
            HasPasswordError = true;
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
