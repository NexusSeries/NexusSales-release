using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NexusSales.UI.Pages
{
    public class NullToVisibilityConverter : IValueConverter
    {
        // Visible if value is null, Collapsed if not null
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}