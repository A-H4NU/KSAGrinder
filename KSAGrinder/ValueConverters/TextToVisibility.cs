using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KSAGrinder.ValueConverters
{
    public class TextToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                if (String.IsNullOrEmpty(text)) return Visibility.Visible;
                else return Visibility.Hidden;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
