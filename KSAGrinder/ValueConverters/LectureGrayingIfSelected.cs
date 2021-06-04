using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KSAGrinder.ValueConverters
{
    public class LectureGrayingIfSelected : IValueConverter
    {
        private static List<(string Code, int Number)> _classList;

        public static void Initialize(List<(string Code, int Number)> classList) => _classList = classList;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string code)
            {
                if (_classList.FindIndex((t) => t.Code == code) != -1)
                {
                    return Brushes.LightGray;
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}