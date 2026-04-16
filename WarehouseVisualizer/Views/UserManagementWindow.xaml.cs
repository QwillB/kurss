using System.Windows;
using WarehouseVisualizer.ViewModels;

namespace WarehouseVisualizer.Views
{
    public partial class UserManagementWindow : Window
    {
        public UserManagementWindow(WarehouseViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += UserManagementWindow_Loaded;
        }

        private void UserManagementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is WarehouseViewModel viewModel)
            {
                viewModel.LoadUsers();
            }
        }
    }
}