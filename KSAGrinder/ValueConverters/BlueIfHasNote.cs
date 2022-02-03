using KSAGrinder.Components;
using KSAGrinder.Statics;

using System;
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
            bool isLightTheme = Properties.Settings.Default.Theme == "Light";

            if (value[0] is string code && value[1] is int grade && value[2] is int number)
            {
                foreach (Class @class in _schedule)
                {
                    if ((@class.Code, @class.Grade, @class.Number) == (code, grade, number))
                        return isLightTheme ? Brushes.LightGray : new SolidColorBrush(Color.FromRgb(98, 98, 98));
                }

                bool overlap = DoesOverlapIfAdded(code, grade, number);
                bool hasNote = !String.IsNullOrEmpty(DataManager.GetClass(code, grade, number).Note);

                if (overlap && hasNote)
                    //return new SolidColorBrush(Color.FromRgb(159, 50, 185));
                    return isLightTheme ? new SolidColorBrush(Color.FromRgb(171, 83, 193)) : new SolidColorBrush(Color.FromRgb(203, 117, 225));
                if (overlap && !hasNote)
                    return Brushes.Red;
                if (!overlap && hasNote)
                    return isLightTheme ? Brushes.Blue : new SolidColorBrush(Color.FromRgb(64, 99, 236));
            }
            return isLightTheme ? Brushes.Black : Brushes.White;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}