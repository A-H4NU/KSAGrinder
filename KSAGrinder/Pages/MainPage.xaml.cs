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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;

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

        public struct LectureStruct
        {
            public string Department { get; set; }
            public string Name { get; set; }
        }

        public enum Department
        {
            All, MathCS, Newton, ChemBio, Human
        }

        private readonly DataSet _data;

        private readonly string _hash;

        private readonly string _windowTitle;

        private readonly MainWindow _main;

        private readonly List<(string Code, int Number)> _classList;

        private string _workingWith = null;

        public static readonly byte[] Key =
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

        private bool _modified = false;

        private bool Modified
        {
            get => _modified;
            set
            {
                _modified = value;
                _main.Title = $"{_windowTitle} - {_workingWith}{(_modified ? "*" : "")}";
            }
        }

        public ObservableCollection<HourStruct> HourCollection { get; private set; } = new ObservableCollection<HourStruct>();

        public ObservableCollection<Department> DepartmentCollection { get; private set; } = new ObservableCollection<Department>();

        public ObservableCollection<LectureStruct> LectureCollection { get; private set; } = new ObservableCollection<LectureStruct>();

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
            ConvertItemToIndex.DG = Timetable;
            Timetable.DataContext = HourCollection;
            Timetable.Loaded += Timetable_Loaded;
            LectureTable.Loaded += LectureTable_Loaded;
            InitializeHourCollection();

            SizeChanged += MainPage_SizeChanged;

            foreach (Department e in Enum.GetValues(typeof(Department)))
            {
                DepartmentCollection.Add(e);
            }
            LoadLectures();
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

        private void LoadLectures()
        {
            var department = (Department)CmbDepartment.SelectedItem;
            var departmentStr = department.ToString();
            var tLecture = _data.Tables["Lecture"];
            var cDepartment = tLecture.Columns["Department"];
            var cName = tLecture.Columns["Name"];
            LectureCollection.Clear();
            foreach (DataRow row in tLecture.Rows)
            {
                if (department == Department.All || departmentStr == (string)row[cDepartment])
                {
                    LectureCollection.Add(new LectureStruct()
                    {
                        Department = (string)row[cDepartment],
                        Name = (string)row[cName]
                    });
                }
            }
        }

        private void SaveXmlInBinary(string filePath)
        {
            #region Construct XmlDocument 

            var xdoc = new XmlDocument();

            var root = xdoc.CreateElement("Timetable");
            var attHash = xdoc.CreateAttribute("Hash");
            attHash.Value = _hash;
            root.Attributes.Append(attHash);
            xdoc.AppendChild(root);

            foreach (var (code, number) in _classList)
            {
                var node = xdoc.CreateElement("Class");
                var nCode = xdoc.CreateElement("Code");
                nCode.InnerText = code;
                node.AppendChild(nCode);
                var nNumber = xdoc.CreateElement("Number");
                nNumber.InnerText = number.ToString();
                node.AppendChild(nNumber);
                root.AppendChild(node);
            }

            string xmlStr = null;
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
                aes.Key = Key;

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
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (string.Equals(child.Name, name, comparisonType))
                    {
                        return child;
                    }
                }
                return null;
            }

            XmlDocument xdoc = new XmlDocument();
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

                using (var cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(Key, iv), CryptoStreamMode.Read))
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
                    var arr = cls.ChildNodes.Cast<XmlNode>().ToArray();
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
            foreach (DataGridTextColumn column in Enumerable.Concat(Timetable.Columns, LectureTable.Columns))
            {
                column.ElementStyle = dataGridElementStyle;
            }
        }

        private void LectureTable_Loaded(object sender, RoutedEventArgs e)
        {
            LectureTable.Columns[0].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            LectureTable.Columns[0].MaxWidth = 90.0;
            LectureTable.Columns[1].Width = new DataGridLength(2, DataGridLengthUnitType.Star);
        }

        private void BtnLoadID_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new LoadFromID(_data);
            dialog.ShowDialog();
            if (dialog.Result != null)
            {
                DataRow result = dialog.Result;
                var tStudent = _data.Tables["Student"];
                var csApplied = tStudent.Columns["Applied"];

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
                var result = MessageBox.Show(
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
                var ofd = new OpenFileDialog();
                ofd.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
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
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Binary files (*.bin)|*.bin";
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

        #endregion

        private void CmbDepartment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadLectures();
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

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
