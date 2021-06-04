using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace KSAGrinder.ValueConverters
{

    /// <summary>
    /// For row headers of "Timetable"
    /// </summary>
    public class ConvertItemToIndex : IValueConverter
    {
        private static DataGrid _dg;

        public static void Initialize(DataGrid dataGrid) => _dg = dataGrid;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                CollectionView cv = _dg.Items;
                int rowIndex = cv.IndexOf(value)+1;

                var label = new Label
                {
                    Content = rowIndex.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                return label;
            }
            catch (Exception e)
            {
                throw new NotImplementedException(e.Message);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}