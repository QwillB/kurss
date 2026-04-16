using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using WarehouseVisualizer.ViewModels;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;
using System.Windows.Controls.Primitives;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;

namespace WarehouseVisualizer.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Для дизайнера создаем ViewModel без пользователя
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = new WarehouseViewModel();
            }
        }

        public MainWindow(User user)
        {
            InitializeComponent();
            DataContext = new WarehouseViewModel(user);
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем фокус на список материалов
            if (MaterialsList != null && MaterialsList.Items.Count > 0)
            {
                MaterialsList.Focus();
            }
        }

        // Обработчик начала перетаскивания из списка материалов
        private void MaterialsList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ListBox? listBox = sender as ListBox;
                if (listBox == null) return;

                Material? material = listBox.SelectedItem as Material;
                if (material != null)
                {
                    // Начинаем перетаскивание
                    DragDrop.DoDragDrop(listBox, material, DragDropEffects.Copy);

                    var viewModel = (WarehouseViewModel)DataContext;
                    if (viewModel != null)
                    {
                        viewModel.SelectedMaterial = material;
                    }
                }
            }
        }

        // Обработчик сброса на ячейку
        private void Cell_Drop(object sender, DragEventArgs e)
        {
            FrameworkElement? element = sender as FrameworkElement;
            if (element == null) return;

            WarehouseCell? cell = element.DataContext as WarehouseCell;
            if (cell == null) return;

            var viewModel = (WarehouseViewModel)DataContext;
            if (viewModel == null) return;

            // Добавление нового материала
            if (e.Data.GetDataPresent(typeof(Material)))
            {
                Material? material = e.Data.GetData(typeof(Material)) as Material;
                if (material == null) return;

                viewModel.SelectedMaterial = material;
                viewModel.DropOnCellCommand?.Execute(cell);
            }
            // Перемещение существующего материала
            else if (e.Data.GetDataPresent(typeof(WarehouseCell)))
            {
                WarehouseCell? sourceCell = e.Data.GetData(typeof(WarehouseCell)) as WarehouseCell;
                if (sourceCell == null) return;

                viewModel.MoveMaterialCommand?.Execute((sourceCell, cell));
            }
        }

        // Обработчик нажатия кнопки мыши на ячейке
        private void Cell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Пропускаем двойной клик - он обрабатывается отдельно
            if (e.ClickCount > 1) return;

            FrameworkElement? element = sender as FrameworkElement;
            if (element == null) return;

            WarehouseCell? cell = element.DataContext as WarehouseCell;
            if (cell == null || cell.Material == null) return;

            var viewModel = (WarehouseViewModel)DataContext;
            if (viewModel != null)
            {
                viewModel.StartDragCommand?.Execute(cell);

                // Передаем саму ячейку для перемещения
                DragDrop.DoDragDrop(element, cell, DragDropEffects.Move);
            }
        }

        // Обработчик контекстного меню для материалов
        private void MaterialListBoxItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            if (listBoxItem != null)
            {
                listBoxItem.IsSelected = true;
                e.Handled = true;
            }
        }

        // Обработчик кнопки быстрого перетаскивания
        private void QuickDragButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Material material)
            {
                var viewModel = (WarehouseViewModel)DataContext;
                if (viewModel != null)
                {
                    viewModel.SelectedMaterial = material;
                    WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                        $"Выбран материал '{material.Name}'. Нажмите на ячейку склада для размещения."));
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Warehouse Visualizer v1.0\nСистема управления складом", "О программе");
        }



        private void TestSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataService = new SqlDataService();

                // Создаем простой склад для теста
                var testWarehouse = new Warehouse
                {
                    Rows = 2,
                    Columns = 2
                };

                testWarehouse.RebuildCells();

                // Добавляем один материал в первую ячейку
                if (testWarehouse.Cells.Count > 0)
                {
                    var firstCell = testWarehouse.Cells[0];
                    firstCell.Material = new Material
                    {
                        Name = "Тестовый материал",
                        Type = MaterialType.Other,
                        Quantity = 10,
                        Unit = "шт."
                    };
                }

                // Пробуем сохранить
                dataService.SaveWarehouse(testWarehouse);

                MessageBox.Show("Тест сохранения выполнен. Проверьте Output окно в VS.",
                    "Тест", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Тестовая ошибка: {ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка теста", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}