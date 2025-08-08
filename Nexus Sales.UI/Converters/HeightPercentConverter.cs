using System;
using System.Globalization;
using System.Windows.Data;

namespace NexusSales.UI.Converters
{
    public class HeightPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double height = System.Convert.ToDouble(value);
            double percent = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            return height * percent;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}