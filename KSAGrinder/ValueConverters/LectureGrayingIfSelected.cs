using KSAGrinder.Components;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KSAGrinder.ValueConverters
{
    public class LectureGrayingIfSelected : IMultiValueConverter
    {
        private static Schedule _schedule;

        public static void Initialize(Schedule schedule) => _schedule = schedule;

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isLightTheme = Properties.Settings.Default.Theme == "Light";

            if (value[0] is string code && value[1] is int grade)
            {
                foreach (var @class in _schedule)
                {
                    if (@class.Code == code && @class.Grade == grade)
                        return isLightTheme ? Brushes.LightGray : new SolidColorBrush(Color.FromRgb(98, 98, 98));
                }
            }
            return isLightTheme ? Brushes.Black : Brushes.White;
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}