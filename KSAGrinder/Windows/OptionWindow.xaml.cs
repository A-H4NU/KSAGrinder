using KSAGrinder.Properties;
using KSAGrinder.Statics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KSAGrinder.Windows
{
    /// <summary>
    /// Interaction logic for Option.xaml
    /// </summary>
    public partial class OptionWindow : Window
    {
        public OptionWindow()
        {
            InitializeComponent();

            ChkRememberDataset.IsChecked = Settings.Default.RememberDataset;
            ChkRememberSave.IsChecked = Settings.Default.RememberSave;
            SldFontSize.Value = Settings.Default.DetailFontSize;

            //foreach (var themeName in ColorFromTheme.ThemeNames)
            //    CmbTheme.Items.Add(themeName);
            //CmbTheme.SelectedIndex = ColorFromTheme.ThemeNames.ToList().IndexOf(Settings.Default.Theme);
        }

        private void ChkRememberDataset_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Settings.Default.RememberDataset = (bool)e.NewValue;
            Settings.Default.Save();
        }

        private void ChkRememberSave_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Settings.Default.RememberSave = (bool)e.NewValue;
            Settings.Default.Save();
        }

        private void SldFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.Default.DetailFontSize = e.NewValue;
            Settings.Default.Save();
        }

        private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.Theme = (string)((ComboBoxItem)CmbTheme.SelectedItem).Content;
            Settings.Default.Save();
        }

        private void CmbLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Settings.Default.Language = 
            Settings.Default.Save();
        }
    }
}
