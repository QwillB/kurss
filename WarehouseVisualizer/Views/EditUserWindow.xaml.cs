using System.Windows;
using System.Windows.Controls;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;

namespace WarehouseVisualizer.Views
{
    public partial class EditUserWindow : Window
    {
        private readonly IAuthService _authService = new AuthService();
        private string _password = "";
        private string _confirmPassword = "";

        public User User { get; private set; }

        // Исправленные свойства - теперь они публичные
        public bool IsNewUser => User.Id == 0;
        public bool CanChangeRole => IsNewUser || User.Role != UserRole.Admin;
        public bool CanDeactivate => User.Role != UserRole.Admin || IsNewUser;

        public EditUserWindow(User user)
        {
            User = user;
            InitializeComponent();
            DataContext = this; // Теперь DataContext = само окно
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _password = ((PasswordBox)sender).Password;
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _confirmPassword = ((PasswordBox)sender).Password;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(User.Username))
            {
                MessageBox.Show("❌ Введите логин пользователя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsNewUser && string.IsNullOrWhiteSpace(_password))
            {
                MessageBox.Show("❌ Введите пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsNewUser && _password != _confirmPassword)
            {
                MessageBox.Show("❌ Пароли не совпадают", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Обновляем пароль если он был изменен
            if (!string.IsNullOrWhiteSpace(_password))
            {
                User.PasswordHash = _authService.HashPassword(_password);
            }

            DialogResult = true;
            Close();
        }
    }
}