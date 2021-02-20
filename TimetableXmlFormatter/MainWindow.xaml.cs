using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

namespace TimetableXmlFormatter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly Dictionary<string, DayOfWeek> KoreanDayToEnum = new Dictionary<string, DayOfWeek>()
        {
            { "월", DayOfWeek.Monday },
            { "화", DayOfWeek.Tuesday },
            { "수", DayOfWeek.Wednesday },
            { "목", DayOfWeek.Thursday },
            { "금", DayOfWeek.Friday },
            { "토", DayOfWeek.Saturday },
            { "일", DayOfWeek.Sunday },
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private string GetCsvFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (ofd.ShowDialog() == true)
                return ofd.FileName;
            return null;
        }

        private void SelectFileWithButton(Button button)
        {
            string filename = GetCsvFile();
            if (filename == null) return;
            DockPanel panel = button.Parent as DockPanel;
            TextBox txt = panel.Children.OfType<TextBox>().First();
            txt.Text = filename;
            txt.ScrollToHorizontalOffset(Double.MaxValue);
        }

        private void BtnClass_Click(object sender, RoutedEventArgs e) => SelectFileWithButton(BtnClass);

        private void BtnCS1_Click(object sender, RoutedEventArgs e) => SelectFileWithButton(BtnCS1);

        private void BtnCS2_Click(object sender, RoutedEventArgs e) => SelectFileWithButton(BtnCS2);

        private void BtnGen_Click(object sender, RoutedEventArgs e)
        {
            TextBox[] txts = new TextBox[] { TxtClass, TxtCS1, TxtCS2 };
            if (txts.Any((txt) => String.IsNullOrEmpty(txt.Text)))
            {
                MessageBox.Show("Please fill in the all blanks.");
            }
            else
            {
                GenerateDataSet(TxtClass.Text, TxtCS1.Text, TxtCS2.Text);
            }
        }

        /// <summary>
        /// Assumed there are CSV files at those paths
        /// </summary>
        private DataSet GenerateDataSet(string classCSVpath, string stdCSVpath1, string stdCSVpath2)
        {
            /*
             * <교과 테이블>
             * 교과목코드, 교과군, 교과목명, 학점, 시수
             * 
             * <분반 테이블>
             * 교과목코드, 분반 번호, 담당교원, 요일/시간, 신청수, 비고
             */
            #region Generating Lecture Table & Class Table

            // TODO: make this not hard-coded (indices)
            var lectureColumns = new (string Name, Type Type, int Index)[]
            {
                ("Code", typeof(string), 3),
                ("Group", typeof(string), 4),
                ("Name", typeof(string), 6),
                ("Credit", typeof(int), 12),
                ("Hours", typeof(int), 13),
            };

            // TODO: make this not hard-coded (indices)
            var classColumns = new (string Name, Type Type, int Index)[]
            {
                ("Code", typeof(string), 3),
                ("Number", typeof(int), 8),
                ("Teacher", typeof(string), 9),
                ("Time", typeof((DayOfWeek day, int hour)[]), 10),
                ("Enrollment", typeof(string), 11),
                ("Note", typeof(string), 14)
            };

            DataTable lectureTable = new DataTable("Lectures");
            foreach (var (name, type, _) in lectureColumns)
            {
                lectureTable.Columns.Add(name, type);
            }

            DataTable classTable = new DataTable("Classes");
            foreach (var (name, type, _) in classColumns)
            {
                classTable.Columns.Add(name, type);
            }

            var classes = new HashSet<string>();
            using (FileStream fs = new FileStream(classCSVpath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    sr.ReadLine();
                    while (!sr.EndOfStream)
                    {
                        string[] values = sr.ReadLine().Split(',');
                        string code = values[lectureColumns[0].Index];
                        if (!classes.Contains(code)) // if the lecture is read first time
                        {
                            classes.Add(code);
                            object[] lectureData = new object[lectureColumns.Length];
                            for (int i = 0; i < lectureData.Length; ++i)
                            {
                                string value = values[lectureColumns[i].Index];
                                lectureData[i] = lectureColumns[i].Type == typeof(string) ? (object)value : Int32.Parse(value);
                            }
                            lectureTable.Rows.Add(lectureData);
                        }
                        object[] classData = new object[classColumns.Length];
                        for (int i = 0; i < classData.Length; ++i)
                        {
                            string value = values[classColumns[i].Index];
                            object datum = null;
                            Type type = classColumns[i].Type;
                            if (type == typeof(string)) datum = value;
                            else if (type == typeof(int)) datum = Int32.Parse(value);
                            else datum = ConvertToTimes(value);
                            classData[i] = datum;
                        }
                        classTable.Rows.Add(classData);
                    }
                }
            }

            #endregion

            DataSet ds = new DataSet("Status");
            ds.Tables.AddRange(new DataTable[]
            {
                lectureTable,
                classTable
            });
            File.WriteAllText(@"C:\Users\HANU\Desktop\xml.xml", ds.GetXml());
            MessageBox.Show("done");
            return ds;
        }

        private (DayOfWeek day, int hour)[] ConvertToTimes(string time)
        {
            string[] times = time.Split('|');
            var result = new (DayOfWeek day, int hour)[times.Length];
            for (int i = 0; i < times.Length; ++i)
            {
                DayOfWeek day = KoreanDayToEnum[times[i].Substring(0, 1)];
                int hour = Int32.Parse(times[i].Substring(times[i].Length - 1));
                result[i] = (day, hour);
            }
            return result;
        }
    }    
}
