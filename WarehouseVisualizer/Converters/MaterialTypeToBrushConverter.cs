using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Converters
{
    public class MaterialTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is MaterialType type)) return Brushes.Transparent;

            switch (type)
            {
                case MaterialType.Cable: return Brushes.Yellow;
                case MaterialType.Pipe: return Brushes.LightBlue;
                case MaterialType.Tool: return Brushes.LightGray;
                case MaterialType.Lumber: return Brushes.Brown;
                case MaterialType.Metal: return Brushes.Silver;
                case MaterialType.Concrete: return Brushes.Gray;
                case MaterialType.Insulation: return Brushes.Pink;
                case MaterialType.Paint: return Brushes.Orange;
                case MaterialType.Other: return Brushes.LightGreen;
                default: return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}