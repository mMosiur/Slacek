using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace Slacek.Client.Desktop
{
    public class ApplicationPageValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ApplicationPage applicationPage)
            {
                switch (applicationPage)
                {
                    case ApplicationPage.Login:
                        return new LoginPage();

                    case ApplicationPage.Chat:
                        return new ChatPage();

                    default:
                        Debugger.Break();
                        return null;
                }
            }
            else throw new InvalidOperationException("The target must be a boolean");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
