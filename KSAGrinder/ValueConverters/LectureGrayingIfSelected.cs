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
            if (value[0] is string code && value[1] is int grade)
            {
                foreach (var @class in _schedule)
                {
                    if (@class.Code == code && @class.Grade == grade)
                        return Brushes.LightGray;
                }
            }
            return Brushes.Black;
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}