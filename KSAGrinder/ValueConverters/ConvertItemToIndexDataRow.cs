using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace KSAGrinder.ValueConverters
{

    /// <summary>
    /// For row headers of "Timetable"
    /// </summary>
    public class ConvertItemToIndexDataRow : IValueConverter
    {
        private static DataGrid _dg;

        public static void Initialize(DataGrid dataGrid) => _dg = dataGrid;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                CollectionView cv = _dg.Items;
                int rowIndex = cv.IndexOf(value) + 1;

                Color foreground;
                switch (Properties.Settings.Default.Theme)
                {
                    case "Light":
                        foreground = Colors.Black;
                        break;
                    default:
                        foreground = Colors.White;
                        break;
                }
                Label label = new Label
                {
                    Content = rowIndex.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    //Background = new SolidColorBrush(Colors.Red),
                    Foreground = new SolidColorBrush(foreground)
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