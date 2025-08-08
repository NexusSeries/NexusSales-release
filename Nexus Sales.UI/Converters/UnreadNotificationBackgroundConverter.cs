using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NexusSales.UI.Converters
{
    public class UnreadNotificationBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRead && !isRead)
            {
                // Light blue background for unread notifications
                return new SolidColorBrush(Color.FromRgb(240, 248, 255));
            }
            // Transparent background for read notifications
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}