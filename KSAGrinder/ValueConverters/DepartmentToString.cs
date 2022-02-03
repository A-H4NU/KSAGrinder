using KSAGrinder.Components;

using System;
using System.Globalization;
using System.Windows.Data;

namespace KSAGrinder.ValueConverters
{
    public class DepartmentToString : IValueConverter
    {
        public static string Convert(Department dep)
        {
            switch (dep)
            {
                case Department.All: return "전체";
                case Department.Human: return "인문";
                case Department.MathCS: return "수리정보";
                case Department.Newton: return "물리지구";
                case Department.ChemBio: return "생물화학";
                case Department.Inter: return "융합";
                default: return "";
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Department dep)
            {
                return Convert(dep);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
