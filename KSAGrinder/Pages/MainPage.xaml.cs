using KSAGrinder.Components;
using KSAGrinder.Exceptions;
using KSAGrinder.Extensions;
using KSAGrinder.Properties;
using KSAGrinder.Statics;
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

        private string _originalScheduleID = String.Empty;

        public string OriginalScheduleID
        {
            get => _originalScheduleID;
            set
            {
                _originalScheduleID = value;
                Schedule.OriginalScheduleID = value;
                UpdateWindowTitle();

            }
        }

        private string _workingWith;

        /// <summary>
        /// Key for en(de)crypting save files
        /// </summary>
        public static readonly byte[] CryptKey =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
            };

        public string WorkingWith
        {
            get => _workingWith;
            private set
            {
                _workingWith = value;
                UpdateWindowTitle();
            }
        }

        private bool _modified;

        private bool Modified
        {
            get => _modified;
            set
            {
                _modified = value;
                UpdateWindowTitle();
            }
        }

        private readonly string _dataSetPath;

        public string DataSetPath { get => _dataSetPath; }

        #region ObservableCollection

        public ObservableCollection<Hour> HourCollection { get; } = new ObservableCollection<Hour>();

        public ObservableCollection<Department> DepartmentCollection { get; } = new ObservableCollection<Department>();

        public ObservableCollection<Lecture> LectureCollection { get; } = new ObservableCollection<Lecture>();

        public ObservableCollection<Class> ClassCollection { get; } = new ObservableCollection<Class>();

        public ObservableCollection<Class> CurrentClassCollection { get; } = new ObservableCollection<Class>();

        public ObservableCollection<string> PreferenceCollection { get; } = new ObservableCollection<string>();

        public ObservableCollection<Schedule> ScheduleCollection { get; } = new ObservableCollection<Schedule>();

        #endregion

        public static readonly RoutedCommand ShortCut_CtrlN = new RoutedCommand();
        public static readonly RoutedCommand ShortCut_CtrlO = new RoutedCommand();
        public static readonly RoutedCommand ShortCut_CtrlS = new RoutedCommand();

        public const double MaxRowHeight = 65.0;

        public const int NRow = 14;

        public MainPage(MainWindow main, string dataSetPath, DataSet data, string hash, string filePathToOpen = null)
        {
            _main = main;
            _data = data;
            _hash = hash;
            _windowTitle = _main.Title;
            _dataSetPath = dataSetPath;

            DataManager.SetData(data);

            _currentSchedule = new Schedule();

            InitializeComponent();

            ConvertItemToIndexDataRow.Initialize(Timetable);
            LectureGrayingIfSelected.Initialize(_currentSchedule);
            BlueIfHasNote.Initialize(_currentSchedule);

            Timetable.DataContext = HourCollection;

            Timetable.Loaded += Timetable_Loaded;
            // Make shortcuts able to be executed from the start
            Timetable.Focus();

            InitializeHourCollection();

            SizeChanged += MainPage_SizeChanged;

            _main.Closing += MainWindow_Closing;

            ScheduleCollection.CollectionChanged += (o, e) => LblNumSchedules.Content = $"총 {ScheduleCollection.Count}개의 시간표를 조합했습니다.";

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

            ShortCut_CtrlN.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            ShortCut_CtrlO.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            ShortCut_CtrlS.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(ShortCut_CtrlN, MenuNewSchedule_Click));
            CommandBindings.Add(new CommandBinding(ShortCut_CtrlO, MenuOpen_Click));
            CommandBindings.Add(new CommandBinding(ShortCut_CtrlS, MenuSave_Click));


            if (!String.IsNullOrWhiteSpace(filePathToOpen))
            {
                try
                {
                    LoadXmlInBinary(filePathToOpen);
                    WorkingWith = filePathToOpen;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"파일을 불러오는 데 실패했습니다!{Environment.NewLine}{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    FileInput.ClearLastFileSettings();
                    return;
                }
            }

            _data.Tables.Remove("Lecture");
            _data.Tables.Remove("Class");
        }

        private void UpdateWindowTitle()
        {
            string title = _windowTitle;
            if (!String.IsNullOrWhiteSpace(OriginalScheduleID))
                title += " - " + OriginalScheduleID;
            if (!String.IsNullOrWhiteSpace(WorkingWith) || Modified)
                title += " - " + WorkingWith + (Modified ? "*" : "");
            _main.Title = title;
        }

        private void UpdateHourCollection()
        {
            string[,] hours = new string[NRow, 5];

            CurrentClassCollection.Clear();
            foreach (Class @class in _currentSchedule)
            {
                string code = @class.Code;
                int grade = @class.Grade;
                int number = @class.Number;

                string classStr = $"{DataManager.GetNameOfLectureFromCode(code)}({grade}){Environment.NewLine}"
                             + $"{number}분반{Environment.NewLine}"
                             + $"{@class.Teacher}";

                foreach ((DayOfWeek day, int hour) in @class.Schedule)
                {
                    if (String.IsNullOrWhiteSpace(hours[hour - 1, (int)day - 1]))
                        hours[hour - 1, (int)day - 1] = classStr;
                    else
                        hours[hour - 1, (int)day - 1] = "!!!!!!!!!\n겹침\n!!!!!!!!!";
                }
                CurrentClassCollection.Add(DataManager.GetClass(code, grade, number));
            }

            HourCollection.Clear();
            for (int i = 0; i < NRow; ++i)
            {
                HourCollection.Add(new Hour()
                {
                    __hour__ = i + 1,
                    Monday = hours[i, 0],
                    Tuesday = hours[i, 1],
                    Wednesday = hours[i, 2],
                    Thursday = hours[i, 3],
                    Friday = hours[i, 4],
                });
            }

            LblValid.Content = _currentSchedule.IsValid ? String.Empty : "유효하지 않음";
            int totalCredit = _currentSchedule.Select(cls => DataManager.GetLecture(cls.Code, cls.Grade).Credit)
                                               .Aggregate(0, (a, b) => a + b);
            LblCredit.Text = $"총 {totalCredit}학점";
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
            var start = DateTime.Now;
            Department department = (Department)CmbDepartment.SelectedItem;
            LectureCollection.Clear();
            bool searchStringAllInitialConsonants = HangulDisassembler.AreInitialConsonants(TxtSearch.Text);
            string disassembledSearchText = HangulDisassembler.Disassemble(TxtSearch.Text);
            foreach (Lecture lecture in DataManager.GetLectures())
            {
                bool departmentCombOk = department == Department.All || department == lecture.Department;
                bool searchOk = String.IsNullOrEmpty(TxtSearch.Text)
                        || lecture.Name.StartsWith(TxtSearch.Text, StringComparison.OrdinalIgnoreCase)
                        || (searchStringAllInitialConsonants
                            ? HangulDisassembler.ExtractInitialConsonants(lecture.Name).StartsWith(TxtSearch.Text)
                            : HangulDisassembler.Disassemble(lecture.Name).StartsWith(disassembledSearchText));
                if (departmentCombOk && searchOk)
                {
                    LectureCollection.Add(lecture);
                }
            }
            System.Diagnostics.Debug.WriteLine((DateTime.Now - start).TotalMilliseconds);
        }

        private void SaveXmlInBinary(string filePath)
        {
            #region Construct XmlDocument 

            XmlDocument xdoc = new XmlDocument();

            XmlElement root = xdoc.CreateElement("Timetable");
            XmlAttribute attHash = xdoc.CreateAttribute("Hash");
            attHash.Value = _hash;
            root.Attributes.Append(attHash);
            XmlAttribute attOrigin = xdoc.CreateAttribute("OriginalID");
            attOrigin.Value = OriginalScheduleID;
            root.Attributes.Append(attOrigin);
            xdoc.AppendChild(root);

            foreach (Class cls in _currentSchedule)
            {
                string code = cls.Code;
                int grade = cls.Grade;
                int number = cls.Number;
                XmlElement node = xdoc.CreateElement("Class");
                XmlElement nCode = xdoc.CreateElement("Code");
                nCode.InnerText = code;
                node.AppendChild(nCode);
                XmlElement nGrade = xdoc.CreateElement("Grade");
                nGrade.InnerText = grade.ToString();
                node.AppendChild(nGrade);
                XmlElement nNumber = xdoc.CreateElement("Number");
                nNumber.InnerText = number.ToString();
                node.AppendChild(nNumber);
                root.AppendChild(node);
            }

            string xmlStr;
            using (StringWriter sw = new StringWriter())
            using (XmlWriter xw = XmlWriter.Create(sw))
            {
                xdoc.WriteTo(xw);
                xw.Flush();
                xmlStr = sw.GetStringBuilder().ToString();
            }

            #endregion

            #region Ecrypt and save the XMLDocument in the specified path

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = CryptKey;
                    byte[] iv = aes.IV;
                    fileStream.Write(iv, 0, iv.Length);

                    using (CryptoStream cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter encryptWriter = new StreamWriter(cryptoStream))
                        {
                            encryptWriter.Write(xmlStr);
                        }
                    }
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

            XmlDocument xdoc = new XmlDocument();

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            using (Aes aes = Aes.Create())
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

                using (CryptoStream cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(CryptKey, iv), CryptoStreamMode.Read))
                {
                    using (StreamReader decryptReader = new StreamReader(cryptoStream))
                    {
                        string decrypted = decryptReader.ReadToEnd();
                        xdoc.LoadXml(decrypted);
                    }
                }
            }

            List<Class> newList = new List<Class>();
            XmlElement root = xdoc.DocumentElement;
            string hash = root.Attributes.GetNamedItem("Hash").Value;
            if (_hash == hash)
            {
                foreach (XmlNode cls in root.ChildNodes)
                {
                    XmlNode[] arr = cls.ChildNodes.Cast<XmlNode>().ToArray();
                    if (arr.Length != 3)
                    {
                        throw new Exception("포맷 에러");
                    }
                    newList.Add(DataManager.GetClass(
                        FindByName(cls, "Code").InnerText,
                        Int32.Parse(FindByName(cls, "Grade").InnerText),
                        Int32.Parse(FindByName(cls, "Number").InnerText)));
                }
                _currentSchedule.Clear();
                _currentSchedule.AddRange(newList);
                UpdateHourCollection();
                ScheduleCollection.Clear();
            }
            else
            {
                throw new DifferentDataSetException("다른 데이터셋에서 만든 파일입니다.");
            }
            OriginalScheduleID = root.Attributes.GetNamedItem("OriginalID").Value;
        }

        public void InvalidateStyles()
        {
            Style lectureRowStyle = LectureTable.RowStyle;
            LectureTable.RowStyle = null;
            LectureTable.RowStyle = lectureRowStyle;

            Style classRowStyle = ClassTable.RowStyle;
            ClassTable.RowStyle = null;
            ClassTable.RowStyle = classRowStyle;

            LectureTable.SelectedIndex = -1;
            ClassTable.SelectedIndex = -1;
            CurrentClassTable.SelectedIndex = -1;
        }

        private void DeleteClassFromCurrentSchedule(Class cls)
        {
            _currentSchedule.Remove(cls);
            UpdateHourCollection();
            InvalidateStyles();

            Modified = true;
        }

        private bool TrySaveDialog()
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "시간표 파일 (*.sch)|*.sch"
                };
                if (sfd.ShowDialog() == true)
                {
                    SaveXmlInBinary(sfd.FileName);
                    WorkingWith = sfd.FileName;
                    Modified = false;
                    Settings.Default.LastFile = sfd.FileName;
                    Settings.Default.Save();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"저장하는 데 실패했습니다!{Environment.NewLine}{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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
            Style dataGridElementStyle = (Style)Resources["LectureTableTextBlockStyle"];
            foreach (DataGridColumn column in Timetable.Columns.Concat(LectureTable.Columns))
            {
                if (column is DataGridTextColumn textColumn)
                    textColumn.ElementStyle = dataGridElementStyle;
            }
            foreach (DataGridColumn column in Timetable.Columns.Concat(SchedulesTable.Columns))
            {
                if (column is DataGridTextColumn textColumn)
                    textColumn.ElementStyle = dataGridElementStyle;
            }
        }

        private void MenuNewSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (Modified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "현재 진행 상황을 폐기하고 새 시간표를 만드시겠습니까?",
                    "새 시간표",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            _currentSchedule.Clear();
            Modified = false;
            OriginalScheduleID = null;
            WorkingWith = null;
            ScheduleCollection.Clear();
            UpdateHourCollection();
            InvalidateStyles();
        }

        private void MenuLoadID_Click(object sender, RoutedEventArgs e)
        {
            if (Modified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "현재 진행 상황을 폐기하고 학번에서 불러오시겠습니까?",
                    "학번에서 불러오기",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            LoadFromID dialog = new LoadFromID(_data);
            dialog.ShowDialog();
            if (dialog.ResultRow != null)
            {
                IEnumerable<Class> newList = DataManager.GetScheduleFromStudentID(dialog.ResultID);

                if (!Enumerable.SequenceEqual(newList, _currentSchedule) || OriginalScheduleID != dialog.ResultID)
                {
                    _currentSchedule.Clear();
                    _currentSchedule.AddRange(newList);
                    UpdateHourCollection();
                    InvalidateStyles();

                    Modified = true;
                    OriginalScheduleID = dialog.ResultID;
                    ScheduleCollection.Clear();
                }
            }
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            if (Modified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "현재 진행 상황을 폐기하고 파일을 불러오시겠습니까?",
                    "불러오기",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            try
            {
                OpenFileDialog ofd = new OpenFileDialog()
                {
                    Filter = "시간표 파일 (*.sch)|*.sch|모든 파일 (*.*)|*.*"
                };
                if (ofd.ShowDialog() == true)
                {
                    LoadXmlInBinary(ofd.FileName);
                    WorkingWith = ofd.FileName;
                    Modified = false;
                }
                if (Settings.Default.RememberSave)
                {
                    Settings.Default.LastFile = ofd.FileName;
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"파일을 불러오는 데 실패했습니다!{Environment.NewLine}{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(WorkingWith))
            {
                TrySaveDialog();
                return;
            }
            try
            {
                SaveXmlInBinary(WorkingWith);
                Modified = false;
            }
            catch
            {
                MessageBox.Show("저장하는 데 실패했습니다!", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e) => TrySaveDialog();

        private void MenuOption_Click(object sender, RoutedEventArgs e)
        {
            OptionWindow optionWindow = new OptionWindow(this);
            optionWindow.ShowDialog();
        }

        private void CmbDepartment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Start");
            LoadLectures();
        }

        private void LectureTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LectureTable.SelectedItem == null)
            {
                return;
            }
            Lecture lecture = (Lecture)LectureTable.SelectedItem;
            ClassCollection.Clear();
            foreach (Class cls in DataManager.ClassDict(lecture.Code, lecture.Grade))
            {
                ClassCollection.Add(cls);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Settings.Default.InstantSearch)
                LoadLectures();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                LoadLectures();
        }

        private void ClassTableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ClassTable.SelectedItem is Class cls)
            {
                string note = String.IsNullOrWhiteSpace(cls.Note) ? "없음" : cls.Note;
                string content =
                    $"강의명: {cls.Name}{Environment.NewLine}" +
                    $"학년: {cls.Grade}{Environment.NewLine}" +
                    $"분반: {cls.Number}{Environment.NewLine}" +
                    $"교과목코드: {cls.Code}{Environment.NewLine}" +
                    $"선생님: {cls.Teacher}{Environment.NewLine}" +
                    $"요일/시간: {cls.DayTime}{Environment.NewLine}" +
                    $"신청인원: {cls.Enroll}{Environment.NewLine}" +
                    $"비고: {note}{Environment.NewLine}{Environment.NewLine}" +
                    $"신청한 학생 목록{Environment.NewLine}";
                foreach (string student in cls.EnrolledList)
                    content += $" - {student} {DataManager.GetNameFromStudentID(student)}\n";
                DetailView detailWindow = new DetailView(content);
                detailWindow.Show();
            }
            else
            {
                MessageBox.Show("세부 정보를 불러오는 데 실패했습니다!", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CurrentClassTableRow_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentClassTable.SelectedItem is Class cls)
            {
                DeleteClassFromCurrentSchedule(cls);
                ScheduleCollection.Clear();
            }
        }

        private void CurrentClassTableRow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && CurrentClassTable.SelectedItem is Class cls)
            {
                DeleteClassFromCurrentSchedule(cls);
            }
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            List<(string, int)> pinned = new List<(string, int)>();
            for (int i = 0; i < _currentSchedule.Count; ++i)
            {
                Class @class = (Class)CurrentClassTable.Items[i];
                if (((CheckBox)CurrentClassTable.GetCell(i, 0).Content).IsChecked == true)
                    pinned.Add((@class.Code, @class.Grade));
            }
            IEnumerable<Schedule> newSchedules = _currentSchedule.Combination(pinned, onlyValid: true);

            ScheduleCollection.Clear();
            foreach (Schedule schedule in newSchedules)
            {
                ScheduleCollection.Add(schedule);
            }
        }

        private void BtnTrade_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentSchedule.IsValid)
            {
                MessageBoxResult result = MessageBox.Show(
                    "현재 시간표가 유효하지 않습니다. 그래도 진행하시겠습니까?",
                    "현재 시간표가 유효하지 않음",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;
            }
            if (String.IsNullOrWhiteSpace(OriginalScheduleID))
            {
                MessageBox.Show("트레이드를 탐색하기 위해서 학번을 입력해야 합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadFromID enterId = new LoadFromID(_data);
                enterId.ShowDialog();
                if (enterId.ResultRow == null)
                    return;
                OriginalScheduleID = enterId.ResultID;
            }
            TradeFinder finder = new TradeFinder(OriginalScheduleID, _currentSchedule);
            finder.ShowDialog();
            //var sch1 = new Schedule(DataManager.GetScheduleFromStudentID("20-050"));
            //Debug.Assert(sch1.MoveClass("HA1804", 6));
            //var sch2 = new Schedule(DataManager.GetScheduleFromStudentID("20-088"));
            //Debug.Assert(sch2.MoveClass("HA1804", 8));
            //var capture = new TradeCapture
            //{
            //    new ClassMove("20-050", "HA1804", 8, 6),
            //    new ClassMove("20-088", "HA1804", 6, 8),
            //};
            //IEnumerable<IEnumerable<ClassMove>> res = ClassMove.GenerateClassMoves(new[]
            //{
            //    ("20-050", sch1),
            //    ("20-088", sch2)
            //},
            //capture,
            //0,
            //2);
            //IEnumerable<ClassMove>[] r = res.ToArray();
            //MessageBox.Show(r.Length.ToString());
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow && (sender as DataGridRow).Item is Class cls)
            {
                Class? @class = null;
                foreach (Class c in _currentSchedule)
                {
                    if ((c.Code, c.Grade) == (cls.Code, cls.Grade))
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
                    _currentSchedule.Remove(@class.Value);
                    _currentSchedule.Add(cls);
                }
                else
                {
                    _currentSchedule.Add(cls);
                }
                UpdateHourCollection();
                InvalidateStyles();
                ScheduleCollection.Clear();

                Modified = true;
            }
        }

        private void SchedulesTable_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column.SortDirection == null)
            {
                e.Column.SortDirection = ListSortDirection.Ascending;
                e.Handled = false;
            }
        }

        private void SchedulesTableRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow && (sender as DataGridRow).Item is Schedule schedule)
            {
                int[] pinnedIndices = (from i in Enumerable.Range(0, _currentSchedule.Count)
                                       where ((CheckBox)CurrentClassTable.GetCell(i, 0).Content).IsChecked == true
                                       select i)
                                      .ToArray();
                schedule.CopyTo(_currentSchedule);

                UpdateHourCollection();
                foreach (int pinnedIndex in pinnedIndices)
                {
                    CheckBox checkBox = CurrentClassTable.GetCell(pinnedIndex, 0).Content as CheckBox;
                    checkBox.IsChecked = true;
                }

                InvalidateStyles();

                Modified = true;
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (Modified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "현재 진행 상황을 저장하고 종료하시겠습니까?",
                    "종료",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question,
                    MessageBoxResult.Cancel);
                switch (result)
                {
                    case MessageBoxResult.No:
                        break;
                    case MessageBoxResult.Yes:
                        if (String.IsNullOrWhiteSpace(WorkingWith) && !TrySaveDialog())
                            e.Cancel = true;
                        else
                            MenuSave_Click(this, null);
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
            _data.Dispose();
        }

        #endregion
    }
}