using KSAGrinder.Components;
using KSAGrinder.Properties;
using KSAGrinder.Statics;
using KSAGrinder.Windows;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for ClassSelection.xaml
    /// </summary>
    public partial class ClassSelection : Page
    {
        private readonly ObservableCollection<ClassCheckBox> _classCheckBoxes = new ObservableCollection<ClassCheckBox>();
        public ObservableCollection<ClassCheckBox> ClassCheckBoxes => _classCheckBoxes;

        private readonly TradeFinder _main;
        private readonly TradeFinderMain _previousPage;

        public ClassSelection(TradeFinder main, TradeFinderMain previousPage)
        {
            InitializeComponent();

            _main = main;
            _previousPage = previousPage;

            foreach (var lectureCode in previousPage.LecturesToMove.Keys)
                _classCheckBoxes.Add(new ClassCheckBox(
                    lectureCode,
                    DataManager.NameOfLectureFromCode(lectureCode),
                    previousPage.LecturesToMove[lectureCode]));
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            foreach (var classCheckBox in _classCheckBoxes)
            {
                _previousPage.LecturesToMove[classCheckBox.Code] = classCheckBox.IsChecked;
            }
            _previousPage.UpdateSelectionMessage();
            _main.Main.Navigate(_previousPage);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            foreach (var classCheckBox in _classCheckBoxes)
            {
                classCheckBox.IsChecked = _previousPage.LecturesToMove[classCheckBox.Code];
            }
            _main.Main.Navigate(_previousPage);
        }

        public class ClassCheckBox : INotifyPropertyChanged
        {

            public event PropertyChangedEventHandler PropertyChanged;
            public string Code { get; private set; }

            public string Name { get; private set; }

            private bool _isChecked;
            public bool IsChecked
            {
                get => _isChecked;
                set
                {
                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }

            public ClassCheckBox(string code, string name, bool isChecked)
            {
                Code = code ?? throw new ArgumentNullException(nameof(code));
                Name = name ?? throw new ArgumentNullException(nameof(name));
                _isChecked = isChecked;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var help = new DetailView(
                String.Format(
                    Properties.Resources.ResourceManager.GetString("ClassSelectionHelp"),
                    $"{_previousPage.StudentId} {DataManager.GetNameFromStudentID(_previousPage.StudentId)}"),
                "도움말",
                TextWrapping.WrapWithOverflow);
            help.ShowDialog();
            e.Handled = true;
        }
    }
}