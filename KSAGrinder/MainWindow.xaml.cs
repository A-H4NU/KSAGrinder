using KSAGrinder.Pages;
using KSAGrinder.Properties;

using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace KSAGrinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly FieldInfo _menuDropAlignmentField;

        public MainWindow()
        {
            string[] themes = new[] { "Light", "Dark", "Black" };
            if (!themes.Contains(Settings.Default.Theme))
            {
                MessageBox.Show($"테마 {Settings.Default.Theme}는 없는 테마입니다.");
                Settings.Default.Theme = themes[0];
            }

            InitializeComponent();

            Main.Content = new FileInput(this);
            _menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            System.Diagnostics.Debug.Assert(_menuDropAlignmentField is not null);

            EnsureStandardPopupAlignment();
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
        }

        private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            EnsureStandardPopupAlignment();
        }

        private void EnsureStandardPopupAlignment()
        {
            if (SystemParameters.MenuDropAlignment && _menuDropAlignmentField is not null)
            {
                _menuDropAlignmentField.SetValue(null, false);
            }
        }
    }
}
