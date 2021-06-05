using KSAGrinder.Components;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KSAGrinder.ValueConverters
{
    public class LectureGrayingIfSelected : IValueConverter
    {
        private static Schedule _schedule;

        public static void Initialize(Schedule schedule) => _schedule = schedule;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string code)
            {
                IEnumerator<Class> enumerator = _schedule.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.Code == code)
                        return Brushes.LightGray;
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}