using System.Windows;
using OfficeOpenXml;

namespace WarehouseVisualizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Инициализация лицензии EPPlus 7.0.10
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
    }
}