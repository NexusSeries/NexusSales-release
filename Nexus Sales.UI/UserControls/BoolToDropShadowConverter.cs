using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Effects;

namespace NexusSales.UI.UserControls
{
    public class BoolToDropShadowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value is bool b && b;
            return isActive ? new DropShadowEffect() : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}