using KSAGrinder.Windows;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace KSAGrinder.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public struct HourStruct
        {
            public string Monday { get; set; }
            public string Tuesday { get; set; }
            public string Wednesday { get; set; }
            public string Thursday { get; set; }
            public string Friday { get; set; }

            public int Hour;
        }

        private readonly DataSet _data;

        private readonly List<(string Code, int Number)> _classList;

        public ObservableCollection<HourStruct> HourCollection { get; private set; } = new ObservableCollection<HourStruct>();

        public const double MaxRowHeight = 50.0;

        public const int NRow = 14;

        public MainPage(DataSet data)
        {
            _data = data;
            _classList = new List<(string Code, int Number)>();

            InitializeComponent();
            ConvertItemToIndex.DG = Timetable;
            Timetable.DataContext = HourCollection;
            Timetable.Loaded += Timetable_Loaded;
            InitializeHourCollection();

            SizeChanged += MainPage_SizeChanged;
        }

        private void UpdateHourCollection()
        {
            var tClass = _data.Tables["Class"];
            var ccCode = tClass.Columns["Code"];
            var ccTeacher = tClass.Columns["Teacher"];
            var ccNumber = tClass.Columns["Number"];
            var ccTime = tClass.Columns["Time"];
            var tLecture = _data.Tables["Lecture"];
            var clName = tLecture.Columns["Name"];

            var hours = new string[NRow, 5];
            
            foreach (var (code, number) in _classList)
            {
                DataRow classRow = null;
                foreach (DataRow row in tClass.Rows)
                {
                    if ((string)row[ccCode] == code && (int)row[ccNumber] == number)
                    {
                        classRow = row;
                        break;
                    }
                }
                DataRow lectureRow = tLecture.Rows.Find(code);

                var classStr = $"{lectureRow[clName]}{Environment.NewLine}"
                             + $"Class #{classRow[ccNumber]}{Environment.NewLine}"
                             + $"{classRow[ccTeacher]}";

                var times = ((DayOfWeek Day, int Hour)[])classRow[ccTime];
                foreach (var (day, hour) in times)
                {
                    hours[hour - 1, (int)day - 1] = classStr;
                }
            }

            HourCollection.Clear();
            for (int i = 0; i < NRow; ++i)
            {
                HourCollection.Add(new HourStruct()
                {
                    Hour        = i + 1,
                    Monday      = hours[i, 0],
                    Tuesday     = hours[i, 1],
                    Wednesday   = hours[i, 2],
                    Thursday    = hours[i, 3],
                    Friday      = hours[i, 4],
                });
            }
        }

        private void InitializeHourCollection()
        {
            HourCollection.Clear();
            for (int i = 0; i < NRow; ++i)
            {
                HourCollection.Add(new HourStruct()
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

        #region Events

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
            double n_rowToShow = Math.Min(NRow, Math.Floor((Timetable.ActualHeight - headerHeight) / MaxRowHeight));
            if (n_rowToShow != 0.0)
            {
                Timetable.RowHeight = (Timetable.ActualHeight - headerHeight) / n_rowToShow;
            }
            else
            {
                Timetable.RowHeight = MaxRowHeight;
            }
        }

        private void Timetable_Loaded(object sender, RoutedEventArgs e)
        {
            Style dataGridElementStyle = (Style)Resources["TextBoxStyle"];
            foreach (DataGridTextColumn column in Timetable.Columns)
            {
                column.ElementStyle = dataGridElementStyle;
            }
        }

        private void BtnLoadID_Click(Object sender, RoutedEventArgs e)
        {

            var dialog = new LoadFromID(_data);
            dialog.ShowDialog();
            if (dialog.Result != null)
            {
                DataRow result = dialog.Result;
                var tStudent = _data.Tables["Student"];
                var csApplied = tStudent.Columns["Applied"];
                var tClass = _data.Tables["Class"];
                var ccCode = tClass.Columns["Code"];
                var ccNumber = tClass.Columns["Number"];
                var ccTime = tClass.Columns["Time"];

                
                _classList.Clear();
                _classList.AddRange(((string Code, int Number)[])result[csApplied]);
                UpdateHourCollection();
            }
        }

        private void BtnLoadFile_Click(Object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnSave_Click(Object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnSaveAs_Click(Object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion
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
