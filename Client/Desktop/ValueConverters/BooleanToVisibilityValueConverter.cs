using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Slacek.Client.Desktop
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                bool preferCollapsed = parameter as bool? ?? false;
                return boolean ? Visibility.Visible : (preferCollapsed ? Visibility.Collapsed : Visibility.Hidden);
            }
            else throw new InvalidOperationException("The target must be a boolean");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            else throw new InvalidOperationException("The target must be a Visibility");
        }
    }
}
