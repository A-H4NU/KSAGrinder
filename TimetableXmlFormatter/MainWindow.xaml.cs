using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TimetableXmlFormatter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string ECEndsWith = "(EC)";

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

        public MainWindow() => InitializeComponent();

        private string GetCsvFile()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                return ofd.FileName;
            }

            return null;
        }

        private void SelectFileWithButton(Button button)
        {
            string filename = GetCsvFile();
            if (filename == null)
            {
                return;
            }

            var panel = button.Parent as DockPanel;
            TextBox txt = panel.Children.OfType<TextBox>().First();
            txt.Text = filename;
            txt.ScrollToHorizontalOffset(Double.MaxValue);
        }

        private void BtnClass_Click(object sender, RoutedEventArgs e) => SelectFileWithButton(BtnClass);

        private void BtnCS1_Click(object sender, RoutedEventArgs e) => SelectFileWithButton(BtnCS1);

        private void BtnCS2_Click(object sender, RoutedEventArgs e) => SelectFileWithButton(BtnCS2);

        private void BtnGen_Click(object sender, RoutedEventArgs e)
        {
            var txts = new TextBox[] { TxtClass, TxtCS1, TxtCS2 };
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
             * 
             * <학생 테이블>
             * 학번, 이름, 듣는 과목/분반 (코드 배열)
             */

            #region Generate Lecture & Class Table

            // TODO: make thses not hard-coded (indices)
            var lectureColumns = new (string Name, Type Type, int Index)[]
            {
                ("Code", typeof(string), 3), // this must be first
                ("Name", typeof(string), 6), // this must be second
                ("Group", typeof(string), 4),
                ("Credit", typeof(int), 12),
                ("Hours", typeof(int), 13),
            };
            var classColumns = new (string Name, Type Type, int Index)[]
            {
                ("Code", typeof(string), 3),
                ("Number", typeof(int), 8),
                ("Teacher", typeof(string), 9),
                ("Time", typeof((DayOfWeek day, int hour)[]), 10),
                ("Enrollment", typeof(string), 11),
                ("Note", typeof(string), 14)
            };

            var lectureTable = new DataTable("Lecture");
            foreach ((String name, Type type, Int32 _) in lectureColumns)
            {
                lectureTable.Columns.Add(name, type);
            }

            lectureTable.PrimaryKey = new DataColumn[] { lectureTable.Columns["Code"] };

            var classTable = new DataTable("Class");
            foreach ((String name, Type type, Int32 _) in classColumns)
            {
                classTable.Columns.Add(name, type);
            }

            var lectures = new Dictionary<string, string>(); // lecture name => code
            using (var fs = new FileStream(classCSVpath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sr = new StreamReader(fs))
            {
                sr.ReadLine(); // skip column headers
                while (!sr.EndOfStream)
                {
                    string[] values = sr.ReadLine().Split(',');
                    string code = values[lectureColumns[0].Index];
                    string name = values[lectureColumns[1].Index];
                    if (!lectures.ContainsValue(code)) // if the lecture is read first time
                    {
                        lectures.Add(name, code);
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
                        Type type = classColumns[i].Type;
                        object datum;
                        if (type == typeof(string))
                        {
                            datum = value;
                        }
                        else if (type == typeof(int))
                        {
                            datum = Int32.Parse(value);
                        }
                        else
                        {
                            datum = ConvertToTimes(value);
                        }

                        classData[i] = datum;
                    }
                    classTable.Rows.Add(classData);
                }
            }

            #endregion

            #region Generate Student Table

            var studentTable = new DataTable("Student");
            studentTable.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("ID", typeof(string)),
                new DataColumn("Name", typeof(string)),
                new DataColumn("Applied", typeof((string Code, int Number)[])),
            });
            studentTable.PrimaryKey = new DataColumn[] { studentTable.Columns["ID"] };

            var processedID = new HashSet<string>();

            void ProcessStudentFile(string path)
            {
                // indices of the first occurences of lecture names (on stdCSVpath1)
                var firstIndex = new Dictionary<int, string>(); // (index, lecture name)
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        string[] firstRow = sr.ReadLine().Split(',');
                        for (int i = 1; i < firstRow.Length; ++i)
                        {
                            string lectureName = GetUntilOrEntire(firstRow[i], "(");
                            if (firstIndex.ContainsValue(lectureName))
                            {
                                continue;
                            }
                            else
                            {
                                firstIndex.Add(i, lectureName);
                            }
                        }
                        sr.ReadLine();
                        while (!sr.EndOfStream)
                        {
                            string[] line = sr.ReadLine().Split(',');
                            if (line[0].Length == 0 || line[0][0] < '0' || line[0][0] > '9')
                            {
                                continue;
                            }

                            string id = GetUntilOrEntire(line[0], "(");
                            string name = GetUntilOrEntire(line[0].Substring(id.Length + 1), ")");

                            if (processedID.Contains(id))
                            {
                                continue;
                            }
                            processedID.Add(id);

                            var applied = new List<(string Code, int Number)>();
                            for (int i = 1; i < line.Length; ++i)
                            {
                                if (line[i] != "1" || !firstRow[i].Contains("_"))
                                {
                                    continue;
                                }

                                string[] @class = firstRow[i].Split('_');
                                string code = lectures[@class[0].Substring(0, @class[0].LastIndexOf('('))];
                                int classNum = Int32.Parse(@class[1]);
                                applied.Add((code, classNum));
                            }
                            studentTable.Rows.Add(id, name, applied.ToArray());
                        }
                    }
                }
            }

            ProcessStudentFile(stdCSVpath1);
            ProcessStudentFile(stdCSVpath2);

            #endregion

            var ds = new DataSet("Status");
            ds.Tables.AddRange(new DataTable[]
            {
                lectureTable,
                classTable,
                studentTable
            });

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string xmlPath = System.IO.Path.Combine(desktop, "data.xml");
            string schPath = System.IO.Path.Combine(desktop, "data.xsd");
            ds.WriteXml(xmlPath, XmlWriteMode.IgnoreSchema);
            ds.WriteXmlSchema(schPath);
            MessageBox.Show("Done");
            return ds;
        }

        private (DayOfWeek day, int hour)[] ConvertToTimes(string time)
        {
            string[] times = time.Split('|');
            var result = new (DayOfWeek day, int hour)[times.Length];
            for (int i = 0; i < times.Length; ++i)
            {
                DayOfWeek day = KoreanDayToEnum[times[i].Substring(0, 1)];
                int hour = int.Parse(times[i].Substring(times[i].Length - 1));
                result[i] = (day, hour);
            }
            return result;
        }

        private static string GetUntilOrEntire(string text, string stopAt)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);
                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }
            return text;
        }
    }    
}
