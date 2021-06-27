﻿using KoreanText;

using KSAGrinder.Components;
using KSAGrinder.Exceptions;
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

        private string _originalScheduleID = String.Empty;

        public string OriginalScheduleID
        {
            get => _originalScheduleID;
            set
            {
                _originalScheduleID = value;
                InvalidateWindowTitle();
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

        private string WorkingWith
        {
            get => _workingWith;
            set
            {
                _workingWith = value;
                InvalidateWindowTitle();
            }
        }

        private bool _modified;
        
        private bool Modified
        {
            get => _modified;
            set
            {
                _modified = value;
                InvalidateWindowTitle();
            }
        }

        #region ObservableCollection

        public ObservableCollection<Hour> HourCollection { get; } = new ObservableCollection<Hour>();

        public ObservableCollection<Department> DepartmentCollection { get; } = new ObservableCollection<Department>();

        public ObservableCollection<Lecture> LectureCollection { get;} = new ObservableCollection<Lecture>();

        public ObservableCollection<Class> ClassCollection { get; } = new ObservableCollection<Class>();

        public ObservableCollection<Class> CurrentClassCollection { get; } = new ObservableCollection<Class>();

        public ObservableCollection<string> PreferenceCollection { get; } = new ObservableCollection<string>();

        public ObservableCollection<Schedule> ScheduleCollection { get; } = new ObservableCollection<Schedule>();

        #endregion

        public static readonly RoutedCommand ShortCut_CtrlN = new RoutedCommand();
        public static readonly RoutedCommand ShortCut_CtrlO = new RoutedCommand();
        public static readonly RoutedCommand ShortCut_CtrlS = new RoutedCommand();


        public const double MaxRowHeight = 50.0;

        public const int NRow = 14;

        public MainPage(MainWindow main, DataSet data, string hash)
        {
            _main = main;
            _data = data;
            _hash = hash;
            _windowTitle = _main.Title;

            DataManager.SetData(data);

            _currentSchedule = new Schedule();

            InitializeComponent();

            ConvertItemToIndex.Initialize(Timetable);
            LectureGrayingIfSelected.Initialize(_currentSchedule);
            BlueIfHasNote.Initialize(_data.Tables["Class"], _currentSchedule);

            Timetable.DataContext = HourCollection;

            Timetable.Loaded += Timetable_Loaded;

            InitializeHourCollection();

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

            ShortCut_CtrlN.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            ShortCut_CtrlO.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            ShortCut_CtrlS.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(ShortCut_CtrlN, MenuNewSchedule_Click));
            CommandBindings.Add(new CommandBinding(ShortCut_CtrlO, MenuOpen_Click));
            CommandBindings.Add(new CommandBinding(ShortCut_CtrlS, MenuSave_Click));
        }

        private void InvalidateWindowTitle()
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
                DataRow classRow = DataManager.GetClassRow(code, number);
                DataRow lectureRow = tLecture.Rows.Find(code);

                string classStr = $"{lectureRow[clName]}{Environment.NewLine}"
                             + $"Class #{classRow[ccNumber]}{Environment.NewLine}"
                             + $"{classRow[ccTeacher]}";

                var times = ((DayOfWeek Day, int Hour)[])classRow[ccTime];
                foreach ((DayOfWeek day, int hour) in times)
                {
                    hours[hour - 1, (int)day - 1] = classStr;
                }
                int idx = DataManager.ClassDict(code).FindIndex((c) => c.Number == number);
                CurrentClassCollection.Add(DataManager.ClassDict(code)[idx]);
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
                        NumClass = DataManager.ClassDict((string)row[cCode]).Count.ToString()
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
            XmlAttribute attOrigin = xdoc.CreateAttribute("OriginalID");
            attOrigin.Value = OriginalScheduleID;
            root.Attributes.Append(attOrigin);
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
            OriginalScheduleID = root.Attributes.GetNamedItem("OriginalID").Value;
            if (_hash == hash)
            {
                foreach (XmlNode cls in root.ChildNodes)
                {
                    XmlNode[] arr = cls.ChildNodes.Cast<XmlNode>().ToArray();
                    if (arr.Length != 2)
                    {
                        throw new Exception("포맷 에러");
                    }
                    newList.Add(DataManager.ClassDict(FindByName(cls, "Code").InnerText)[Int32.Parse(FindByName(cls, "Number").InnerText)-1]);
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
        }

        private void InvalidateStyles()
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
            var dialog = new LoadFromID(_data);
            dialog.ShowDialog();
            if (dialog.ResultRow != null)
            {
                IEnumerable<Class> newList = DataManager.GetScheduleFromID(dialog.ResultID);

                if (!Enumerable.SequenceEqual(newList, _currentSchedule) || OriginalScheduleID != dialog.ResultID)
                {
                    _currentSchedule.Clear();
                    _currentSchedule.AddRange(newList);
                    UpdateHourCollection();

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
                var ofd = new OpenFileDialog()
                {
                    Filter = "바이너리 파일 (*.bin)|*.bin|모든 파일 (*.*)|*.*"
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

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(WorkingWith))
            {
                MenuSaveAs_Click(sender, e);
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

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "바이너리 파일 (*.bin)|*.bin"
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
            foreach (Class cls in DataManager.ClassDict(lecture.Code))
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
            Class[] notPinned = (from i in Enumerable.Range(0, _currentSchedule.Count)
                                 where ((CheckBox)CurrentClassTable.GetCell(i, 0).Content).IsChecked == false
                                 select (Class)CurrentClassTable.Items[i])
                                .ToArray();
            int n_notPinned = notPinned.Length;
            var sequences = new List<IEnumerable<int>>(n_notPinned);
            foreach (Class @class in notPinned)
                sequences.Add(Enumerable.Range(0, DataManager.ClassDict(@class.Code).Count));

            IEnumerable<int[]> validCombinations =
                sequences.CartesianProduct()
                .Select(i => i.ToArray())
                .Where(
                    combination =>
                    {
                        var schedule = new HashSet<(DayOfWeek, int)>();
                        for (int i = 0; i < n_notPinned; ++i)
                        {
                            foreach ((DayOfWeek Day, int Hour) hour in DataManager.ClassDict(notPinned[i].Code)[combination[i]].Schedule)
                            {
                                if (!schedule.Add(hour))
                                    return false;
                            }
                        }
                        return true;
                    }
                );

            var newSchedules = new List<Schedule>();
            foreach (int[] combination in validCombinations)
            {
                var classes = _currentSchedule.ToList();
                for (int i = 0; i < n_notPinned; ++i)
                {
                    int index = classes.FindIndex(c => c.Code == notPinned[i].Code);
                    Class @class = DataManager.ClassDict(notPinned[i].Code)[combination[i]];
                    classes.RemoveAt(index);
                    classes.Insert(index, @class);
                }
                newSchedules.Add(new Schedule(classes));
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

        private void BtnTrade_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(OriginalScheduleID))
            {
                // TODO: Implement this
                MessageBox.Show("");
            }
            var originalSchedule = DataManager.GetScheduleFromID(OriginalScheduleID).ToList();
            var toTrade = new List<string>(); // list of codes to trade
            foreach (Class cls1 in _currentSchedule)
            {
                foreach (Class cls2 in originalSchedule)
                {
                    if (cls1.Code == cls2.Code)
                    {
                        toTrade.Add(cls1.Code);
                        break;
                    }
                }
            }
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
                InvalidateStyles();
                ScheduleCollection.Clear();

                Modified = true;
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
                    var checkBox = CurrentClassTable.GetCell(pinnedIndex, 0).Content as CheckBox;
                    checkBox.IsChecked = true;
                }

                InvalidateStyles();

                Modified = true;
            }
        }

        #endregion
    }
}