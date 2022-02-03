using KSAGrinder.Pages;
using KSAGrinder.Properties;

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KSAGrinder.Windows
{
    /// <summary>
    /// Interaction logic for Option.xaml
    /// </summary>
    public partial class OptionWindow : Window
    {
        private static readonly string[] _themeNames =
        {
            "Light",
            "Dark",
            "Black"
        };

        private readonly MainPage _mainPage;

        public OptionWindow(MainPage mainPage)
        {
            InitializeComponent();

            _mainPage = mainPage;

            ChkRememberDataset.IsChecked = Settings.Default.RememberDataset;
            ChkRememberSave.IsChecked = Settings.Default.RememberSave;
            SldFontSize.Value = Settings.Default.DetailFontSize;

            SldFontSize.ValueChanged += SldFontSize_ValueChanged;

            foreach (string themeName in _themeNames)
                CmbTheme.Items.Add(themeName);
            CmbTheme.SelectedIndex = _themeNames.ToList().IndexOf(Settings.Default.Theme);
        }

        private void SldFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.Default.DetailFontSize = e.NewValue;
            Settings.Default.Save();
        }

        private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.Theme = (string)CmbTheme.SelectedItem;
            Settings.Default.Save();
            _mainPage.InvalidateStyles();
        }

        private void ChkRememberDataset_Unchecked(object sender, RoutedEventArgs e)
        {
            ChkRememberSave.IsChecked = false;
        }

        private void ChkRememberSave_Checked(object sender, RoutedEventArgs e)
        {
            ChkRememberDataset.IsChecked = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.RememberDataset = ChkRememberDataset.IsChecked.Value;
            Settings.Default.RememberSave = ChkRememberSave.IsChecked.Value;

            if (Settings.Default.RememberDataset)
                Settings.Default.LastDataset = _mainPage.DataSetPath;
            else
                Settings.Default.LastDataset = null;

            if (Settings.Default.RememberSave)
                Settings.Default.LastFile = _mainPage.WorkingWith;
            else
                Settings.Default.LastFile = null;

            Settings.Default.Save();
        }
    }
}
