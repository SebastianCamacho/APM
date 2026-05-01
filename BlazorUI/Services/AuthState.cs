using System;

namespace BlazorUI.Services
{
    public class AuthState
    {
        public bool IsAuthenticated { get; private set; }

        public event Action? OnChange;

        public void EnsureAuthenticated()
        {
            IsAuthenticated = true;
            NotifyStateChanged();
        }

        public void Logout()
        {
            IsAuthenticated = false;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
