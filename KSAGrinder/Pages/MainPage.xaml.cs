using KoreanText;

using KSAGrinder.Windows;

using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace KSAGrinder.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public struct Hour
        {
            public string Monday { get; set; }
            public string Tuesday { get; set; }
            public string Wednesday { get; set; }
            public string Thursday { get; set; }
            public string Friday { get; set; }

            public int __hour__;
        }

        public struct Lecture
        {
            public string Code { get; set; }
            public string Department { get; set; }
            public string Name { get; set; }

            public string NumClass { get; set; }
        }

        public struct Class
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public string Number { get; set; }
            public string Teacher { get; set; }

            public (DayOfWeek Day, int Hour)[] Schedule { get; set; }
            public string DayTime
            {
                get
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < Schedule.Length; ++i)
                    {
                        if (i != 0) sb.Append(" ");
                        sb.Append(Schedule[i].Day.ToString().Substring(0, 3).ToUpper());
                        sb.Append(Schedule[i].Hour);
                    }
                    return sb.ToString();
                }
            }
            public string Enroll { get; set; }
            public string Note { get; set; }

            public List<string> EnrolledList { get; set; }
        }

        public enum DepartmentEnum
        {
            All, MathCS, Newton, ChemBio, Human, Inter
        }

        private readonly DataSet _data;

        private readonly string _hash;

        private readonly string _windowTitle;

        private readonly MainWindow _main;

        private readonly List<(string Code, int Number)> _classList;

        private string _workingWith;

        public static readonly byte[] CryptKey =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
            };

        private string WorkingWith
        {
            get => _workingWith;
            set
            {
                _workingWith = value;
                _main.Title = $"{_windowTitle} - {_workingWith}";
            }
        }

        private bool _modified;
        
        private bool Modified
        {
            get => _modified;
            set
            {
                _modified = value;
                _main.Title = $"{_windowTitle} - {_workingWith}{(_modified ? "*" : "")}";
            }
        }

        public ObservableCollection<Hour> HourCollection { get; private set; } = new ObservableCollection<Hour>();

        public ObservableCollection<DepartmentEnum> DepartmentCollection { get; private set; } = new ObservableCollection<DepartmentEnum>();

        public ObservableCollection<Lecture> LectureCollection { get; private set; } = new ObservableCollection<Lecture>();

        public ObservableCollection<Class> ClassCollection { get; private set; } = new ObservableCollection<Class>();

        public ObservableCollection<Class> CurrentClassCollection { get; private set; } = new ObservableCollection<Class>();

        /// <summary>
        /// correspond a code to a list of classes
        /// </summary>
        private readonly Dictionary<string, List<Class>> _classDict = new Dictionary<string, List<Class>>();

        public const double MaxRowHeight = 50.0;

        public const int NRow = 14;

        public MainPage(MainWindow main, DataSet data, string hash)
        {
            _main = main;
            _data = data;
            _hash = hash;
            _windowTitle = _main.Title;

            _classList = new List<(string Code, int Number)>();

            InitializeComponent();

            ConvertItemToIndex.Initialize(Timetable);
            LectureGrayingIfSelected.Initialize(_classList);
            BlueIfHasNote.Initialize(_data.Tables["Class"], _classList, _classDict);

            Timetable.DataContext = HourCollection;

            Timetable.Loaded += Timetable_Loaded;

            InitializeHourCollection();
            InitializeClassDictionary();

            SizeChanged += MainPage_SizeChanged;

            foreach (DepartmentEnum e in Enum.GetValues(typeof(DepartmentEnum)))
            {
                DepartmentCollection.Add(e);
            }
            LoadLectures();
        }

        private DataRow GetClassRow(string code, int number)
        {
            DataTable tClass = _data.Tables["Class"];
            DataColumn ccCode = tClass.Columns["Code"];
            DataColumn ccNumber = tClass.Columns["Number"];

            DataRow classRow = null;
            foreach (DataRow row in tClass.Rows)
            {
                if ((string)row[ccCode] == code && (int)row[ccNumber] == number)
                {
                    classRow = row;
                    break;
                }
            }
            return classRow;
        }

        private void InitializeClassDictionary()
        {

            DataTable tLecture = _data.Tables["Lecture"];
            DataColumn cName = tLecture.Columns["Name"];
            DataTable tClass = _data.Tables["Class"];
            DataColumn cCode = tClass.Columns["Code"];
            DataColumn cNumber = tClass.Columns["Number"];
            DataColumn cTeacher = tClass.Columns["Teacher"];
            DataColumn cTime = tClass.Columns["Time"];
            DataColumn cEnroll = tClass.Columns["Enrollment"];
            DataColumn cNote = tClass.Columns["Note"];
            DataTable tStudent = _data.Tables["Student"];
            var applyDict = new Dictionary<(string Code, int Number), List<string>>();
            void AddToApplyDict(string code, int number, string student)
            {
                if (applyDict.TryGetValue((code, number), out List<string> list))
                    list.Add(student);
                else
                    applyDict[(code, number)] = new List<string>() { student };
            }
            foreach (DataRow student in tStudent.Rows)
            {
                var applied = ((string Code, int Number)[]) student[tStudent.Columns["Applied"]];
                string idNum = $"{student[tStudent.Columns["ID"]]} {student[tStudent.Columns["Name"]]}";
                foreach ((string code, int number) in applied)
                    AddToApplyDict(code, number, idNum);
            }
            foreach (DataRow row in tClass.Rows)
            {
                string code = (string)row[cCode];

                if (!_classDict.ContainsKey(code))
                {
                    _classDict[code] = new List<Class>();
                }
                _classDict[code].Add(new Class()
                {
                    Code = code,
                    Name = tLecture.Rows.Find(code)[cName].ToString(),
                    Number = row[cNumber].ToString(),
                    Teacher = row[cTeacher].ToString(),
                    Enroll = row[cEnroll].ToString(),
                    Schedule = ((DayOfWeek Day, int Hour)[])row[cTime],
                    Note = row[cNote].ToString(),
                    EnrolledList = applyDict[(code, (int)row[cNumber])]
                });
            }
        }

        private void UpdateHourCollection()
        {
            DataTable tClass = _data.Tables["Class"];
            DataColumn ccTeacher = tClass.Columns["Teacher"];
            DataColumn ccNumber = tClass.Columns["Number"];
            DataColumn ccTime = tClass.Columns["Time"];
            DataTable tLecture = _data.Tables["Lecture"];
            DataColumn clName = tLecture.Columns["Name"];

            string[,] hours = new string[NRow, 5];

            CurrentClassCollection.Clear();
            foreach ((string code, int number) in _classList)
            {
                DataRow classRow = GetClassRow(code, number);
                DataRow lectureRow = tLecture.Rows.Find(code);

                string classStr = $"{lectureRow[clName]}{Environment.NewLine}"
                             + $"Class #{classRow[ccNumber]}{Environment.NewLine}"
                             + $"{classRow[ccTeacher]}";

                var times = ((DayOfWeek Day, int Hour)[])classRow[ccTime];
                foreach ((DayOfWeek day, int hour) in times)
                {
                    hours[hour - 1, (int)day - 1] = classStr;
                }
                int idx = _classDict[code].FindIndex((c) => c.Number == number.ToString());
                CurrentClassCollection.Add(_classDict[code][idx]);
            }

            HourCollection.Clear();
            for (int i = 0; i < NRow; ++i)
            {
                HourCollection.Add(new Hour()
                {
                    __hour__    = i + 1,
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
                HourCollection.Add(new Hour()
                {
                    __hour__ = i + 1,
                    Monday = String.Empty,
                    Tuesday = String.Empty,
                    Wednesday = String.Empty,
                    Thursday = String.Empty,
                    Friday = String.Empty,
                });
            }
        }

        private void LoadLectures()
        {
            string ExtractChosung(KoreanString str)
            {
                string result = "";
                for (int i = 0; i < str.Length; ++i)
                {
                    result += str[i].GetChoSung();
                }
                return result;
            }
            var department = (DepartmentEnum)CmbDepartment.SelectedItem;
            string departmentStr = department.ToString();
            DataTable tLecture = _data.Tables["Lecture"];
            DataColumn cDepartment = tLecture.Columns["Department"];
            DataColumn cName = tLecture.Columns["Name"];
            DataColumn cCode = tLecture.Columns["Code"];
            var newList = new List<Lecture>();
            foreach (DataRow row in tLecture.Rows)
            {
                string name = (string)row[cName];
                var kname = new KoreanString(name);
                if (department == DepartmentEnum.All || departmentStr == (string)row[cDepartment])
                {
                    if (!String.IsNullOrEmpty(TxtSearch.Text)
                        && !(name.StartsWith(TxtSearch.Text, StringComparison.OrdinalIgnoreCase)
                             || ExtractChosung(kname).StartsWith(TxtSearch.Text)))
                    {
                        continue;
                    }
                    newList.Add(new Lecture()
                    {
                        Department = (string)row[cDepartment],
                        Code = (string)row[cCode],
                        Name = name,
                        NumClass = _classDict[(string)row[cCode]].Count.ToString()
                    });
                }
            }
            LectureCollection.Clear();
            LectureTable.InvalidateVisual();
            foreach (Lecture lecture in newList)
            {
                LectureCollection.Add(lecture);
            }
        }

        private void SaveXmlInBinary(string filePath)
        {
            #region Construct XmlDocument 

            var xdoc = new XmlDocument();

            XmlElement root = xdoc.CreateElement("Timetable");
            XmlAttribute attHash = xdoc.CreateAttribute("Hash");
            attHash.Value = _hash;
            root.Attributes.Append(attHash);
            xdoc.AppendChild(root);

            foreach ((string code, int number) in _classList)
            {
                XmlElement node = xdoc.CreateElement("Class");
                XmlElement nCode = xdoc.CreateElement("Code");
                nCode.InnerText = code;
                node.AppendChild(nCode);
                XmlElement nNumber = xdoc.CreateElement("Number");
                nNumber.InnerText = number.ToString();
                node.AppendChild(nNumber);
                root.AppendChild(node);
            }

            string xmlStr;
            using (var sw = new StringWriter())
            using (var xw = XmlWriter.Create(sw))
            {
                xdoc.WriteTo(xw);
                xw.Flush();
                xmlStr = sw.GetStringBuilder().ToString();
            }

            #endregion

            #region Ecrypt and save the XMLDocument in the specified path

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var aes = Aes.Create())
            {
                aes.Key = CryptKey;

                byte[] iv = aes.IV;
                fileStream.Write(iv, 0, iv.Length);

                using (var cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var encryptWriter = new StreamWriter(cryptoStream))
                {
                    encryptWriter.Write(xmlStr);
                }
            }

            #endregion
        }

        private void LoadXmlInBinary(string filePath)
        {
            // Find the first child node with the specified name
            XmlNode FindByName(XmlNode node, string name, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
                => node.ChildNodes.Cast<XmlNode>()
                    .First(child => String.Equals(child.Name, name, comparisonType));

            var xdoc = new XmlDocument();
            using (var fileStream = new FileStream(filePath, FileMode.Open))
            using (var aes = Aes.Create())
            {
                byte[] iv = new byte[aes.IV.Length];
                int numBytesToRead = aes.IV.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    int n = fileStream.Read(iv, numBytesRead, numBytesToRead);
                    if (n == 0) break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }

                using (var cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(CryptKey, iv), CryptoStreamMode.Read))
                using (var decryptReader = new StreamReader(cryptoStream))
                {
                    string decrypted = decryptReader.ReadToEnd();
                    xdoc.LoadXml(decrypted);
                }
            }

            var newList = new List<(string Code, int Number)>();
            XmlElement root = xdoc.DocumentElement;
            string hash = root.Attributes.GetNamedItem("Hash").Value;
            if (_hash == hash)
            {
                foreach (XmlNode cls in root.ChildNodes)
                {
                    XmlNode[] arr = cls.ChildNodes.Cast<XmlNode>().ToArray();
                    if (arr.Length != 2)
                    {
                        throw new Exception("Format error.");
                    }
                    newList.Add((FindByName(cls, "Code").InnerText, Int32.Parse(FindByName(cls, "Number").InnerText)));
                }
                _classList.Clear();
                _classList.AddRange(newList);
                UpdateHourCollection();
            }
            else
            {
                throw new Exception("The file might be from a different dataset.");
            }
        }

        #region Events

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double headerHeight = Double.NaN;
            foreach (SetterBase setter in Timetable.ColumnHeaderStyle.Setters)
            {
                if (setter is Setter s && s.Property.Name == "Height")
                {
                    headerHeight = (double)s.Value;
                    break;
                }
            }
            double nRowToShow = Math.Min(NRow, Math.Floor((Timetable.ActualHeight - headerHeight) / MaxRowHeight));
            if (nRowToShow != 0.0)
            {
                Timetable.RowHeight = (Timetable.ActualHeight - headerHeight) / nRowToShow;
            }
            else
            {
                Timetable.RowHeight = MaxRowHeight;
            }
        }

        private void Timetable_Loaded(object sender, RoutedEventArgs e)
        {
            var dataGridElementStyle = (Style)Resources["TextBoxStyle"];
            foreach (DataGridColumn column in Timetable.Columns.Concat(LectureTable.Columns))
            {
                if (column is DataGridTextColumn textColumn)
                    textColumn.ElementStyle = dataGridElementStyle;
            }
        }

        private void BtnLoadID_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new LoadFromID(_data);
            dialog.ShowDialog();
            if (dialog.Result != null)
            {
                DataRow result = dialog.Result;
                DataTable tStudent = _data.Tables["Student"];
                DataColumn csApplied = tStudent.Columns["Applied"];

                var newList = ((string Code, int Number)[])result[csApplied];
                
                if (!Enumerable.SequenceEqual(newList, _classList))
                {
                    _classList.Clear();
                    _classList.AddRange(newList);
                    UpdateHourCollection();

                    Modified = true;
                }
            }
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (Modified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Want to discard the current progress and open a new file?",
                    "Opening a new file when modifying",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            try
            {
                var ofd = new OpenFileDialog()
                {
                    Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*"
                };
                if (ofd.ShowDialog() == true)
                {
                    LoadXmlInBinary(ofd.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load the file.{Environment.NewLine}{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (WorkingWith == null)
            {
                BtnSaveAs_Click(sender, e);
                return;
            }
            try
            {
                SaveXmlInBinary(WorkingWith);
                Modified = false;
            }
            catch
            {
                MessageBox.Show("Failed to save!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "Binary files (*.bin)|*.bin"
                };
                if (sfd.ShowDialog() == true)
                {
                    SaveXmlInBinary(sfd.FileName);
                    WorkingWith = sfd.FileName;
                    Modified = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save!{Environment.NewLine}{ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbDepartment_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadLectures();

        private void LectureTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LectureTable.SelectedItem == null)
            {
                return;
            }
            var lecture = (Lecture)LectureTable.SelectedItem;
            ClassCollection.Clear();
            foreach (Class cls in _classDict[lecture.Code])
            {
                ClassCollection.Add(cls);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadLectures();

        private void ClassTableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ClassTable.SelectedItem is Class cls)
            {
                string content =
                    $"< {cls.Name} #{cls.Number} >\n\n" +
                    $"Code: {cls.Code}\n" +
                    $"Teacher: {cls.Teacher}\n" +
                    $"Schedule: {cls.DayTime}\n" +
                    $"# Enrollment: {cls.Enroll}\n" +
                    $"Note: {cls.Note}\n\n" +
                    $"Who enrolled?\n";
                foreach (string student in cls.EnrolledList)
                    content += $" - {student}\n";

                MessageBox.Show(content, "Detail", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Cannot show the detail", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGridRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow && (sender as DataGridRow).Item is Class cls)
            {
                (string Code, int) tuple = (cls.Code, Int32.Parse(cls.Number));
                int lectureIdx = _classList.FindIndex((t) => t.Code == cls.Code);
                if (_classList.Contains(tuple))
                {
                    _classList.Remove(tuple);
                }
                else if (lectureIdx != -1)
                {
                    _classList.RemoveAt(lectureIdx);
                    _classList.Add(tuple);
                }
                else
                {
                    _classList.Add(tuple);
                }
                UpdateHourCollection();

                LectureTable_SelectionChanged(this, null);
                int firstIdx = LectureCollection.ToList().FindIndex((l) => l.Code == cls.Code);
                LectureTable.SelectedIndex = firstIdx;
                LoadLectures();
            }
        }

        private void CurrentClassTableRow_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentClassTable.SelectedItem is Class cls)
            {
                _classList.Remove((cls.Code, Int32.Parse(cls.Number)));
                UpdateHourCollection();

                LectureTable_SelectionChanged(this, null);
                int firstIdx = LectureCollection.ToList().FindIndex((l) => l.Code == cls.Code);
                LectureTable.SelectedIndex = firstIdx;
                LoadLectures();
            }
        }

        #endregion

        private void CurrentClassTableRow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (CurrentClassTable.SelectedItem is Class cls)
                {
                    _classList.Remove((cls.Code, Int32.Parse(cls.Number)));
                    UpdateHourCollection();

                    LectureTable_SelectionChanged(this, null);
                    int firstIdx = LectureCollection.ToList().FindIndex((l) => l.Code == cls.Code);
                    LectureTable.SelectedIndex = firstIdx;
                    LoadLectures();
                }
            }
        }
    }

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

    public class LectureGrayingIfSelected : IValueConverter
    {
        private static List<(string Code, int Number)> _classList;

        public static void Initialize(List<(string Code, int Number)> classList) => _classList = classList;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string code)
            {
                if (_classList.FindIndex((t) => t.Code == code) != -1)
                {
                    return Brushes.LightGray;
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class BlueIfHasNote : IMultiValueConverter
    {
        private static DataTable _classTable;
        private static List<(string Code, int Number)> _classList;
        private static Dictionary<string, List<MainPage.Class>> _classDict;

        public static void Initialize(DataTable classTable, List<(string Code, int Number)> classList, Dictionary<string, List<MainPage.Class>> classDict)
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
