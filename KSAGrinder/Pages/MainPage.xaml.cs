using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KSAGrinder.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page, INotifyPropertyChanged
    {
        //public ConvertItemToIndex ConvertItemToIndex { get; private set; }

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

        private readonly DataSet _data;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage(DataSet data)
        {
            InitializeComponent();
            ConvertItemToIndex.DG = Timetable;

            _data = data;
            Timetable.DataContext = HourCollection;
            Timetable.Loaded += Timetable_Loaded;
            InitializeHourCollection();

            this.SizeChanged += MainPage_SizeChanged;

            //ConvertItemToIndex = new ConvertItemToIndex(Timetable);
        }

        private void MainPage_SizeChanged(Object sender, SizeChangedEventArgs e)
        {
        }

        private void Timetable_Loaded(object sender, RoutedEventArgs e)
        {
            double columnHeight = double.NaN;
            foreach (var setter in Timetable.ColumnHeaderStyle.Setters)
            {
                if (setter is Setter s && s.Property.Name == "Height")
                {
                    columnHeight = (double)s.Value;
                    break;
                }
            }
            Timetable.RowHeight = (Timetable.ActualHeight - columnHeight) / 10.0;
        }

        private void InitializeHourCollection()
        {
            HourCollection.Clear();
            for (int i = 0; i < 14; ++i)
            {
                HourCollection.Add(new StructHour()
                {
                    Hour = i + 1,
                    Monday = "Test",
                    Tuesday = "Test",
                    Wednesday = "Test",
                    Thursday = "Test",
                    Friday = "Test",
                });
            }
        }
    }
    public class ConvertItemToIndex : IValueConverter
    {
        public static DataGrid DG;

        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            try
            {
                //MainPage page = (MainPage)Application.Current.MainWindow.FindName("Mainpage");
                //DataGrid dg = (DataGrid)page.FindName("Timetable");
                CollectionView cv = DG.Items;
                //Get the index of the item from the CollectionView
                int rowindex = cv.IndexOf(value)+1;

                Label label = new Label();
                label.Content = rowindex.ToString();
                label.HorizontalAlignment = HorizontalAlignment.Center;
                return label;
            }
            catch (Exception e)
            {
                throw new NotImplementedException(e.Message);
            }
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
