using System;
using System.Globalization;
using System.Windows.Data;
using NexusSales.UI.Dialogs;
using System.Windows.Media;

namespace NexusSales.UI.Converters
{
    public class DebugConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
#if DEBUG
            var dialog = new MessageDialog(
                $"Binding value: {value}",
                "Debug",
                soundFileName: "Success.wav",
                titleColor: (Brush)System.Windows.Application.Current.FindResource("FontNormalBrush")
            );
            dialog.ShowDialog();
#endif
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}