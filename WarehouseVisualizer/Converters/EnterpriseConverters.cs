using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Converters
{
    public class StringEqualsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CellOverlayBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var cell = values.Length > 0 ? values[0] as WarehouseCell : null;
            var suggested = values.Length > 1 ? values[1] as WarehouseCell : null;
            var heatmapEnabled = values.Length > 2 && values[2] is bool enabled && enabled;
            var mode = values.Length > 3 ? values[3]?.ToString() ?? string.Empty : string.Empty;

            if (cell == null)
            {
                return Brushes.Transparent;
            }

            if (ReferenceEquals(cell, suggested))
            {
                return new SolidColorBrush(Color.FromArgb(220, 70, 220, 160));
            }

            if (!heatmapEnabled)
            {
                return Brushes.Transparent;
            }

            if (mode.Contains("Активность", StringComparison.OrdinalIgnoreCase))
            {
                var activity = ((cell.Row + 1) * 17 + (cell.Column + 1) * 29) % 100;
                return activity > 66
                    ? new SolidColorBrush(Color.FromArgb(150, 239, 68, 68))
                    : activity > 33
                        ? new SolidColorBrush(Color.FromArgb(120, 245, 158, 11))
                        : new SolidColorBrush(Color.FromArgb(70, 34, 197, 94));
            }

            if (mode.Contains("низкого", StringComparison.OrdinalIgnoreCase))
            {
                return cell.Material?.Quantity <= 5
                    ? new SolidColorBrush(Color.FromArgb(170, 239, 68, 68))
                    : new SolidColorBrush(Color.FromArgb(35, 34, 197, 94));
            }

            if (mode.Contains("высокой", StringComparison.OrdinalIgnoreCase))
            {
                var pressure = Math.Max(0, 6 - Math.Abs(cell.Row - 3) - Math.Abs(cell.Column - 3));
                return pressure > 4
                    ? new SolidColorBrush(Color.FromArgb(155, 239, 68, 68))
                    : pressure > 2
                        ? new SolidColorBrush(Color.FromArgb(115, 245, 158, 11))
                        : new SolidColorBrush(Color.FromArgb(45, 14, 165, 233));
            }

            if (mode.Contains("категор", StringComparison.OrdinalIgnoreCase) && cell.Material != null)
            {
                return cell.Material.Type switch
                {
                    MaterialType.Pipe => new SolidColorBrush(Color.FromArgb(130, 56, 189, 248)),
                    MaterialType.Tool => new SolidColorBrush(Color.FromArgb(130, 167, 139, 250)),
                    MaterialType.Metal => new SolidColorBrush(Color.FromArgb(130, 148, 163, 184)),
                    MaterialType.Cable => new SolidColorBrush(Color.FromArgb(130, 250, 204, 21)),
                    _ => new SolidColorBrush(Color.FromArgb(115, 34, 197, 94))
                };
            }

            return cell.HasMaterial
                ? new SolidColorBrush(Color.FromArgb(125, 245, 158, 11))
                : new SolidColorBrush(Color.FromArgb(55, 34, 197, 94));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotificationPriorityBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                NotificationPriority.Critical => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                NotificationPriority.High => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                NotificationPriority.Medium => new SolidColorBrush(Color.FromRgb(14, 165, 233)),
                _ => new SolidColorBrush(Color.FromRgb(34, 197, 94))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumRussianNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                MaterialType.Cable => "\u041a\u0430\u0431\u0435\u043b\u044c",
                MaterialType.Pipe => "\u0422\u0440\u0443\u0431\u044b",
                MaterialType.Tool => "\u0418\u043d\u0441\u0442\u0440\u0443\u043c\u0435\u043d\u0442\u044b",
                MaterialType.Lumber => "\u041f\u0438\u043b\u043e\u043c\u0430\u0442\u0435\u0440\u0438\u0430\u043b\u044b",
                MaterialType.Metal => "\u041c\u0435\u0442\u0430\u043b\u043b",
                MaterialType.Concrete => "\u0411\u0435\u0442\u043e\u043d",
                MaterialType.Insulation => "\u0418\u0437\u043e\u043b\u044f\u0446\u0438\u044f",
                MaterialType.Paint => "\u041a\u0440\u0430\u0441\u043a\u0430",
                MaterialType.Other => "\u041f\u0440\u043e\u0447\u0435\u0435",
                MaterialStatus.Active => "\u0410\u043a\u0442\u0438\u0432\u0435\u043d",
                MaterialStatus.Reserved => "\u0417\u0430\u0440\u0435\u0437\u0435\u0440\u0432\u0438\u0440\u043e\u0432\u0430\u043d",
                MaterialStatus.Archived => "\u0410\u0440\u0445\u0438\u0432",
                MaterialStatus.Damaged => "\u041f\u043e\u0432\u0440\u0435\u0436\u0434\u0451\u043d",
                MaterialHistoryActionType.Created => "\u0421\u043e\u0437\u0434\u0430\u043d\u0438\u0435",
                MaterialHistoryActionType.Updated => "\u0418\u0437\u043c\u0435\u043d\u0435\u043d\u0438\u0435",
                MaterialHistoryActionType.Moved => "\u041f\u0435\u0440\u0435\u043c\u0435\u0449\u0435\u043d\u0438\u0435",
                MaterialHistoryActionType.Deleted => "\u0423\u0434\u0430\u043b\u0435\u043d\u0438\u0435",
                MaterialHistoryActionType.QuantityChanged => "\u0418\u0437\u043c\u0435\u043d\u0435\u043d\u0438\u0435 \u043a\u043e\u043b\u0438\u0447\u0435\u0441\u0442\u0432\u0430",
                MaterialHistoryActionType.Assigned => "\u041d\u0430\u0437\u043d\u0430\u0447\u0435\u043d\u0438\u0435",
                MaterialHistoryActionType.Restored => "\u0412\u043e\u0441\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u0438\u0435",
                NotificationType.Info => "\u0418\u043d\u0444\u043e\u0440\u043c\u0430\u0446\u0438\u044f",
                NotificationType.Warning => "\u041f\u0440\u0435\u0434\u0443\u043f\u0440\u0435\u0436\u0434\u0435\u043d\u0438\u0435",
                NotificationType.Error => "\u041e\u0448\u0438\u0431\u043a\u0430",
                NotificationType.Success => "\u0423\u0441\u043f\u0435\u0448\u043d\u043e",
                NotificationType.LowStock => "\u041d\u0438\u0437\u043a\u0438\u0439 \u043e\u0441\u0442\u0430\u0442\u043e\u043a",
                NotificationType.WarehouseFull => "\u0421\u043a\u043b\u0430\u0434 \u0437\u0430\u043f\u043e\u043b\u043d\u0435\u043d",
                NotificationType.MaterialMoved => "\u041c\u0430\u0442\u0435\u0440\u0438\u0430\u043b \u043f\u0435\u0440\u0435\u043c\u0435\u0449\u0451\u043d",
                NotificationType.UnauthorizedActionAttempt => "\u041f\u043e\u043f\u044b\u0442\u043a\u0430 \u0434\u043e\u0441\u0442\u0443\u043f\u0430",
                NotificationType.ReportGenerated => "\u041e\u0442\u0447\u0451\u0442 \u0441\u0444\u043e\u0440\u043c\u0438\u0440\u043e\u0432\u0430\u043d",
                NotificationType.BackupCompleted => "\u0420\u0435\u0437\u0435\u0440\u0432\u043d\u0430\u044f \u043a\u043e\u043f\u0438\u044f",
                NotificationType.PlacementSuggestion => "\u041f\u0440\u0435\u0434\u043b\u043e\u0436\u0435\u043d\u0438\u0435 \u0440\u0430\u0437\u043c\u0435\u0449\u0435\u043d\u0438\u044f",
                NotificationType.SystemWarning => "\u0421\u0438\u0441\u0442\u0435\u043c\u043d\u043e\u0435 \u043f\u0440\u0435\u0434\u0443\u043f\u0440\u0435\u0436\u0434\u0435\u043d\u0438\u0435",
                NotificationPriority.Low => "\u041d\u0438\u0437\u043a\u0438\u0439",
                NotificationPriority.Medium => "\u0421\u0440\u0435\u0434\u043d\u0438\u0439",
                NotificationPriority.High => "\u0412\u044b\u0441\u043e\u043a\u0438\u0439",
                NotificationPriority.Critical => "\u041a\u0440\u0438\u0442\u0438\u0447\u0435\u0441\u043a\u0438\u0439",
                _ => value?.ToString() ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
