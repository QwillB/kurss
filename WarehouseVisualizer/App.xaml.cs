using System;
using System.Windows;
using OfficeOpenXml;
using WarehouseVisualizer.Services;

namespace WarehouseVisualizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Инициализация лицензии EPPlus 7.0.10
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using var context = new WarehouseDbContext();
                context.EnsureDiplomaSchema();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось обновить структуру базы данных:\n{ex.Message}",
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }
    }
}
