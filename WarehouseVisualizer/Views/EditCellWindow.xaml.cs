using System.Windows;
using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Views
{
    public partial class EditMaterialWindow : Window
    {
        public Material Material { get; private set; }

        public EditMaterialWindow(Material material)
        {
            Material = material;
            InitializeComponent();
            DataContext = Material;
            Loaded += EditMaterialWindow_Loaded;
        }

        private void EditMaterialWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем фокус на первое поле
            if (Material != null && string.IsNullOrWhiteSpace(Material.Name))
            {
                var nameTextBox = this.FindName("NameTextBox") as System.Windows.Controls.TextBox;
                nameTextBox?.Focus();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(Material.Name))
            {
                MessageBox.Show("❌ Введите название материала", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Material.Quantity <= 0)
            {
                MessageBox.Show("❌ Количество должно быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Material.Unit))
            {
                MessageBox.Show("❌ Введите единицу измерения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}