using System;
using System.Globalization;
using System.Windows.Data;

namespace NexusSales.UI.Converters
{
    public class WidthPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = System.Convert.ToDouble(value);
            double percent = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            return width * percent;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}