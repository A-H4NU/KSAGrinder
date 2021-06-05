using KoreanText;

using KSAGrinder.Components;
using KSAGrinder.Extensions;
using KSAGrinder.ValueConverters;
using KSAGrinder.Windows;

using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

namespace KSAGrinder.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private readonly DataSet _data;

        private readonly string _hash;

        private readonly string _windowTitle;

        private readonly MainWindow _main;

        private readonly Schedule _currentSchedule;

        private string _workingWith;

        /// <summary>
        /// Key for en(de)crypting save files
        /// </summary>
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

        #region Observable Collections

        public ObservableCollection<Hour> HourCollection { get; } = new ObservableCollection<Hour>();

        public ObservableCollection<Department> DepartmentCollection { get; } = new ObservableCollection<Department>();

        public ObservableCollection<Lecture> LectureCollection { get;} = new ObservableCollection<Lecture>();

        public ObservableCollection<Class> ClassCollection { get; } = new ObservableCollection<Class>();

        public ObservableCollection<Class> CurrentClassCollection { get; } = new ObservableCollection<Class>();

        public ObservableCollection<string> PreferenceCollection { get; } = new ObservableCollection<string>();

        public ObservableCollection<Schedule> ScheduleCollection { get; } = new ObservableCollection<Schedule>();

        #endregion

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

            _currentSchedule = new Schedule();

            InitializeComponent();

            ConvertItemToIndex.Initialize(Timetable);
            LectureGrayingIfSelected.Initialize(_currentSchedule);
            BlueIfHasNote.Initialize(_data.Tables["Class"], _currentSchedule, _classDict);

            Timetable.DataContext = HourCollection;

            Timetable.Loaded += Timetable_Loaded;

            InitializeHourCollection();
            InitializeClassDictionary();

            SizeChanged += MainPage_SizeChanged;

            foreach (Department e in Enum.GetValues(typeof(Department)))
            {
                DepartmentCollection.Add(e);
            }
            foreach (Preference e in Enum.GetValues(typeof(Preference)))
            {
                try
                {
                    FieldInfo fInfo = typeof(Preference).GetField(e.ToString());
                    if (fInfo.GetCustomAttributes(typeof(DescriptionAttribute)) is DescriptionAttribute[] attributes && attributes.Any())
                    {
                        PreferenceCollection.Add(attributes.First().Description);
                    }
                }
                catch
                {
                    PreferenceCollection.Add(e.ToString());
                }
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
                    break; ;
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
                    Number = Int32.Parse(row[cNumber].ToString()),
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
            foreach (Class @class in _currentSchedule)
            {
                string code = @class.Code;
                int number = @class.Number;
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
                int idx = _classDict[code].FindIndex((c) => c.Number == number);
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
            var department = (Department)CmbDepartment.SelectedItem;
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
                if (department == Department.All || departmentStr == (string)row[cDepartment])
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

            foreach (Class cls in _currentSchedule)
            {
                string code = cls.Code;
                int number = cls.Number;
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

            var newList = new List<Class>();
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
                    newList.Add(_classDict[FindByName(cls, "Code").InnerText][Int32.Parse(FindByName(cls, "Number").InnerText)-1]);
                }
                _currentSchedule.Clear();
                _currentSchedule.AddRange(newList);
                UpdateHourCollection();
            }
            else
            {
                throw new Exception("다른 데이터셋에서 만든 파일입니다.");
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

                IEnumerable<Class> newList = from tuple in ((string Code, int Number)[])result[csApplied]
                                             select _classDict[tuple.Code][tuple.Number - 1];

                if (!Enumerable.SequenceEqual(newList, _currentSchedule))
                {
                    _currentSchedule.Clear();
                    _currentSchedule.AddRange(newList);
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
                    "현재 진행 상황을 폐기하고 새 파일을 여시겠습니까?",
                    "편집 중 새 파일 열기",
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
                    WorkingWith = ofd.FileName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"파일을 불러오는 데 실패했습니다!{Environment.NewLine}{ex.Message}", "에러",
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
                MessageBox.Show("저장하는 데 실패했습니다!", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    $"저장하는 데 실패했습니다!{Environment.NewLine}{ex.Message}", "에러", 
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
                    $"교과목코드: {cls.Code}\n" +
                    $"선생님: {cls.Teacher}\n" +
                    $"요일/시간: {cls.DayTime}\n" +
                    $"신청자 수: {cls.Enroll}\n" +
                    $"비고: {cls.Note}\n\n" +
                    $"신청한 사람\n";
                foreach (string student in cls.EnrolledList)
                    content += $" - {student}\n";

                MessageBox.Show(content, "세부 정보", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("디테일을 불러오는 데 실패했습니다!", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGridRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow && (sender as DataGridRow).Item is Class cls)
            {
                Class @class = null;
                foreach (Class c in _currentSchedule)
                {
                    if (c.Code == cls.Code)
                    {
                        @class = c;
                        break;
                    }
                }
                if (_currentSchedule.Contains(cls))
                {
                    _currentSchedule.Remove(cls);
                }
                else if (@class != null)
                {
                    _currentSchedule.Remove(@class);
                    _currentSchedule.Add(cls);
                }
                else
                {
                    _currentSchedule.Add(cls);
                }
                UpdateHourCollection();

                LectureTable_SelectionChanged(this, null);
                int firstIdx = LectureCollection.ToList().FindIndex((l) => l.Code == cls.Code);
                LectureTable.SelectedIndex = firstIdx;
                LoadLectures();

                Modified = true;
            }
        }

        private void CurrentClassTableRow_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentClassTable.SelectedItem is Class cls)
            {
                _currentSchedule.Remove(cls);
                UpdateHourCollection();

                LectureTable_SelectionChanged(this, null);
                int firstIdx = LectureCollection.ToList().FindIndex((l) => l.Code == cls.Code);
                LectureTable.SelectedIndex = firstIdx;
                LoadLectures();

                Modified = true;
            }
        }

        private void CurrentClassTableRow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && CurrentClassTable.SelectedItem is Class cls)
            {
                _currentSchedule.Remove(cls);
                UpdateHourCollection();

                LectureTable_SelectionChanged(this, null);
                int firstIdx = LectureCollection.ToList().FindIndex((l) => l.Code == cls.Code);
                LectureTable.SelectedIndex = firstIdx;
                LoadLectures();

                Modified = true;
            }
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            Class[] classList = _currentSchedule.ToArray();
            var sequences = new List<IEnumerable<int>>(_currentSchedule.Count);
            foreach (Class @class in _currentSchedule)
                sequences.Add(Enumerable.Range(0, _classDict[@class.Code].Count));
            IEnumerable<int[]> validCombinations =
                sequences.CartesianProduct()
                .Select(i => i.ToArray())
                .Where(
                    combination =>
                    {
                        var schedule = new HashSet<(DayOfWeek, int)>();
                        for (int i = 0; i < _currentSchedule.Count; ++i)
                        {
                            foreach ((DayOfWeek Day, int Hour) hour in _classDict[classList[i].Code][combination[i]].Schedule)
                            {
                                if (schedule.Contains(hour))
                                {
                                    return false;
                                }
                                _=schedule.Add(hour);
                            }
                        }
                        return true;
                    });
            var newSchedules = new List<Schedule>();
            foreach (int[] combination in validCombinations)
            {
                var schedule = new Schedule();
                for (int i = 0; i < _currentSchedule.Count; ++i)
                {
                    schedule.Add(_classDict[classList[i].Code][combination[i]]);
                }
                newSchedules.Add(schedule);
            }
            switch ((Preference)CmbPreference.SelectedIndex)
            {
                case Preference.Empty1:
                    newSchedules.Sort((a, b) => -Math.Sign(a.Evaluate1Empty - b.Evaluate1Empty));
                    break;
                case Preference.Empty4:
                    newSchedules.Sort((a, b) => -Math.Sign(a.Evaluate4Empty - b.Evaluate4Empty));
                    break;
                case Preference.Empty5:
                    newSchedules.Sort((a, b) => -Math.Sign(a.Evaluate5Empty - b.Evaluate5Empty));
                    break;
                case Preference.Compact:
                    newSchedules.Sort((a, b) => -Math.Sign(a.EvaluateCompact - b.EvaluateCompact));
                    break;
            }
            ScheduleCollection.Clear();
            foreach (Schedule schedule in newSchedules)
            {
                ScheduleCollection.Add(schedule);
            }
        }

        #endregion
    }
}