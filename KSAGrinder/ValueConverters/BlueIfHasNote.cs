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
        private static DataTable _classTable;
        private static Schedule _schedule;

        public static void Initialize(DataTable classTable, Schedule schedule)
        {
            _classTable = classTable;
            _schedule = schedule;
        }

        private static bool DoesOverlapIfAdded(string code, int number)
        {
            (DayOfWeek Day, int Hour)[] GetSchedule(string c, int n)
            {
                int idx = DataManager.ClassDict(c).FindIndex((cls) => cls.Number == n);
                return DataManager.ClassDict(c)[idx].Schedule;
            }
            (DayOfWeek Day, int Hour)[] schedule = GetSchedule(code, number);
            foreach (Class cls in _schedule)
            {
                (DayOfWeek Day, int Hour)[] existingSchedule = GetSchedule(cls.Code, cls.Number);
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
            if (value[0] is string code && value[1] is int number)
            {
                foreach (Class @class in _schedule)
                {
                    if (@class.Code == code && @class.Number == number)
                        return Brushes.LightGray;
                }

                bool overlap = DoesOverlapIfAdded(code, number);
                DataRow classRow = null;
                foreach (DataRow row in _classTable.Rows)
                {
                    if (row[_classTable.Columns["Code"]].Equals(code) && row[_classTable.Columns["Number"]].Equals(number))
                    {
                        classRow = row;
                        break;
                    }
                }

                bool hasNote = classRow != null && !String.IsNullOrEmpty((string)classRow[_classTable.Columns["Note"]]);
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