using System;
using System.Globalization;
using System.Windows.Data;
using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Converters
{
    public class UserRoleToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserRole role)
            {
                return role switch
                {
                    UserRole.Admin => "Администратор",
                    UserRole.Storekeeper => "Кладовщик",
                    UserRole.Auditor => "Аудитор",
                    _ => "Неизвестно"
                };
            }
            return "Неизвестно";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}