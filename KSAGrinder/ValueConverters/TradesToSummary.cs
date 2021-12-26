using KSAGrinder.Components;
using KSAGrinder.Statics;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace KSAGrinder.ValueConverters
{
    public class TradesToSummary : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
                return value as string;
            if (value is ReadOnlyCollection<ClassMove> moves)
            {
                HashSet<string> involvedStudents = moves.Select(move => move.StudentId).ToHashSet();
                involvedStudents.Remove(moves.First().StudentId);
                return $"{involvedStudents.Count() + 1}인 참여 / {moves.Count}번의 분반 이동{Environment.NewLine}"
                  + "참여 학생: " + String.Join(", ", involvedStudents.Select(id => id + " " + DataManager.GetNameFromStudentID(id)));
            }
            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
