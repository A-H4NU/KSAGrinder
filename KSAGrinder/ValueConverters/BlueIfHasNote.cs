using KSAGrinder.Components;

using System;
using System.Collections.Generic;
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
        private static List<(string Code, int Number)> _classList;
        private static Dictionary<string, List<Class>> _classDict;

        public static void Initialize(DataTable classTable, List<(string Code, int Number)> classList, Dictionary<string, List<Class>> classDict)
        {
            _classTable = classTable;
            _classList = classList;
            _classDict = classDict;
        }

        private bool DoesOverlapIfAdded(string code, int number)
        {
            (DayOfWeek Day, int Hour)[] GetSchedule(string c, int n)
            {
                int idx = _classDict[c].FindIndex((cls) => cls.Number == n.ToString());
                return _classDict[c][idx].Schedule;
            }
            (DayOfWeek Day, int Hour)[] schedule = GetSchedule(code, number);
            foreach ((string Code, int Number) cls in _classList)
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
            if (value[0] is string code && value[1] is string number)
            {
                int n = Int32.Parse(number);
                if (_classList.FindIndex((t) => t.Code == code && t.Number == n) != -1)
                {
                    return Brushes.LightGray;
                }
                bool overlap = DoesOverlapIfAdded(code, n);
                DataRow classRow = null;
                foreach (DataRow row in _classTable.Rows)
                {
                    if (row[_classTable.Columns["Code"]].Equals(code) && row[_classTable.Columns["Number"]].Equals(n))
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