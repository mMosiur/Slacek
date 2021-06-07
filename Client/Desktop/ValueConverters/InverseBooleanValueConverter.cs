using System;
using System.Globalization;
using System.Windows.Data;

namespace Slacek.Client.Desktop
{
    [ValueConversion(typeof(bool), typeof(bool))]
    internal class InverseBooleanValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                return !boolean;
            }
            else throw new InvalidOperationException("The target must be a boolean");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                return !boolean;
            }
            else throw new InvalidOperationException("The target must be a boolean");
        }
    }
}
