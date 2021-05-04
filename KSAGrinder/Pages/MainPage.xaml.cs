using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace KSAGrinder.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page, INotifyPropertyChanged
    {
        public struct StructHour
        {
            public string Monday { get; set; }
            public string Tuesday { get; set; }
            public string Wednesday { get; set; }
            public string Thursday { get; set; }
            public string Friday { get; set; }

            public int Hour;
        }

        public string TestStr { get; set; } = "Test";

        public ObservableCollection<StructHour> HourCollection { get; private set; } = new ObservableCollection<StructHour>();

        public event PropertyChangedEventHandler PropertyChanged;

        public const double MinRowHeight = 50.0;

        public MainPage(DataSet data)
        {
            InitializeComponent();
            ConvertItemToIndex.DG = Timetable;
            Timetable.DataContext = HourCollection;
            Timetable.Loaded += Timetable_Loaded;
            InitializeHourCollection();

            SizeChanged += MainPage_SizeChanged;
        }

        private void MainPage_SizeChanged(Object sender, SizeChangedEventArgs e)
        {
            double headerHeight = double.NaN;
            foreach (SetterBase setter in Timetable.ColumnHeaderStyle.Setters)
            {
                if (setter is Setter s && s.Property.Name == "Height")
                {
                    headerHeight = (double)s.Value;
                    break;
                }
            }
            double n_rowToShow = Math.Floor((Timetable.ActualHeight - headerHeight) / MinRowHeight);
            Timetable.RowHeight = (Timetable.ActualHeight - headerHeight) / n_rowToShow;
        }

        private void Timetable_Loaded(object sender, RoutedEventArgs e)
        {
            Style dataGridElementStyle = (Style)Resources["TextBoxStyle"];
            foreach (DataGridTextColumn column in Timetable.Columns)
            {
                column.ElementStyle = dataGridElementStyle;
            }
        }

        private void InitializeHourCollection()
        {
            HourCollection.Clear();
            for (int i = 0; i < 14; ++i)
            {
                HourCollection.Add(new StructHour()
                {
                    Hour = i + 1,
                    Monday = String.Empty,
                    Tuesday = String.Empty,
                    Wednesday = String.Empty,
                    Thursday = String.Empty,
                    Friday = String.Empty,
                });
            }
        }
    }
    
    /// <summary>
    /// For row headers of "Timetable"
    /// </summary>
    public class ConvertItemToIndex : IValueConverter
    {
        public static DataGrid DG;

        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            try
            {
                CollectionView cv = DG.Items;
                int rowindex = cv.IndexOf(value)+1;

                Label label = new Label
                {
                    Content = rowindex.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                return label;
            }
            catch (Exception e)
            {
                throw new NotImplementedException(e.Message);
            }
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
