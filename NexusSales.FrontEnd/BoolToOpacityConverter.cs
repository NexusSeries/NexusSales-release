using System;
using System.Globalization;
using System.Windows.Data;

namespace NexusSales.FrontEnd.Converters
{
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = (bool)value;
            string[] opacityValues = parameter?.ToString().Split(':') ?? new string[] { "0.3", "0" };

            double trueValue = double.Parse(opacityValues[0], CultureInfo.InvariantCulture);
            double falseValue = double.Parse(opacityValues[1], CultureInfo.InvariantCulture);

            return isActive ? trueValue : falseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}