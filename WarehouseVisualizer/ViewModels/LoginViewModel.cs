using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;
using WarehouseVisualizer.Views;

namespace WarehouseVisualizer.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _username = "admin";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _password = "";

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private bool _isLoading;

        public LoginViewModel()
        {
            _authService = new AuthService();
        }

        // В файле ViewModels\LoginViewModel.cs измените метод Login:
        [RelayCommand(CanExecute = nameof(CanLogin))]
        private void Login()  // Убрали async Task
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = "";

                var user = _authService.Authenticate(Username, Password);

                if (user == null)
                {
                    ErrorMessage = "Неверный логин или пароль";
                    HasError = true;
                    return;
                }

                if (!user.IsActive)
                {
                    ErrorMessage = "Пользователь заблокирован";
                    HasError = true;
                    return;
                }

                // Успешная авторизация
                var mainWindow = new MainWindow(user);
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();

                // Закрываем окно входа
                this.CloseWindow();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка авторизации: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !IsLoading;
        }

        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is LoginWindow loginWindow)
                {
                    loginWindow.Close();
                    break;
                }
            }
        }
    }
}