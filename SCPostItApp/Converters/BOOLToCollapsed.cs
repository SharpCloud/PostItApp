using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SC.PostItApp.Converters
{
    public class BoolToCollapsed : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return ((bool) value) ? Visibility.Collapsed : Visibility.Visible;
            if (value is int)
                return ((int) value > 0)  ? Visibility.Collapsed : Visibility.Visible;
            else
                return (value != null) ? Visibility.Collapsed : Visibility.Visible;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
