using System;
using System.Globalization;
using System.Windows.Data;

namespace WarehouseVisualizer.Converters
{
    public class BoolToNewUserConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isNewUser)
            {
                return isNewUser ? "Создание" : "Редактирование";
            }
            return "Редактирование";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}