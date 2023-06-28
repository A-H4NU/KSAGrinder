using KSAGrinder.Statics;
using KSAGrinder.Windows;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace KSAGrinder.Pages
{
    /// <summary>
    /// Interaction logic for ClassSelection.xaml
    /// </summary>
    public partial class ClassSelection : Page
    {
        private readonly ObservableCollection<ClassCheckBox> _classCheckBoxes = new();
        public ObservableCollection<ClassCheckBox> ClassCheckBoxes => _classCheckBoxes;

        private readonly TradeFinder _main;
        private readonly TradeFinderMain _previousPage;

        public ClassSelection(TradeFinder main, TradeFinderMain previousPage)
        {
            InitializeComponent();

            _main = main;
            _previousPage = previousPage;

            foreach ((string code, int grade) in previousPage.LecturesToMove.Keys)
                _classCheckBoxes.Add(new ClassCheckBox(
                    code,
                    grade,
                    DataManager.GetNameOfLectureFromCode(code),
                    previousPage.LecturesToMove[(code, grade)]));
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            foreach (ClassCheckBox ccb in _classCheckBoxes)
            {
                _previousPage.LecturesToMove[(ccb.Code, ccb.Grade)] = ccb.IsChecked;
            }
            _previousPage.UpdateSelectionMessage();
            _main.Main.Navigate(_previousPage);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            foreach (ClassCheckBox ccb in _classCheckBoxes)
            {
                ccb.IsChecked = _previousPage.LecturesToMove[(ccb.Code, ccb.Grade)];
            }
            _main.Main.Navigate(_previousPage);
        }

        public class ClassCheckBox : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public string Code { get; private set; }

            public int Grade { get; private set; }

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

            public ClassCheckBox(string code, int grade, string name, bool isChecked)
            {
                Code = code ?? throw new ArgumentNullException(nameof(code));
                Name = name ?? throw new ArgumentNullException(nameof(name));
                _isChecked = isChecked;
                Grade = grade;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            DetailView help = new(
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