using KSAGrinder.Components;
using KSAGrinder.Statics;

using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace KSAGrinder.ValueConverters
{
    public class BlueIfHasNote : IMultiValueConverter
    {
        private static Schedule _schedule;

        public static void Initialize(Schedule schedule)
        {
            _schedule = schedule;
        }

        private static bool DoesOverlapIfAdded(string code, int grade, int number)
        {
            (DayOfWeek Day, int Hour)[] GetSchedule(string c, int g, int n)
            {
                int idx = DataManager.ClassDict(c, g).FindIndex((cls) => cls.Number == n);
                return DataManager.ClassDict(c, g)[idx].Schedule;
            }
            (DayOfWeek Day, int Hour)[] schedule = GetSchedule(code, grade, number);
            foreach (Class cls in _schedule)
            {
                (DayOfWeek Day, int Hour)[] existingSchedule = GetSchedule(cls.Code, cls.Grade, cls.Number);
                foreach ((DayOfWeek Day, int Hour) time in schedule)
                {
                    if (existingSchedule.Contains(time))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value[0] is string code && value[1] is int grade && value[2] is int number)
            {
                foreach (Class @class in _schedule)
                {
                    if ((@class.Code, @class.Grade, @class.Number) == (code, grade, number))
                        return Brushes.LightGray;
                }

                bool overlap = DoesOverlapIfAdded(code, grade, number);
                bool hasNote = !String.IsNullOrEmpty(DataManager.GetClass(code, grade, number).Note);

                if (overlap && hasNote)
                    return Brushes.PaleVioletRed;
                if (overlap && !hasNote)
                    return Brushes.Red;
                if (!overlap && hasNote)
                    return Brushes.Blue;
            }
            return Brushes.Black;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}