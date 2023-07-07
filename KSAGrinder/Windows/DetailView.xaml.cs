using System;
using System.Windows;

namespace KSAGrinder.Windows
{
    /// <summary>
    /// Interaction logic for TradeDetail.xaml
    /// </summary>
    public partial class DetailView : Window
    {
        public DetailView(string detail, string? title = null, TextWrapping wrapping = TextWrapping.NoWrap)
        {
            InitializeComponent();
            TxtDetail.Text = detail;
            TxtDetail.FontSize = Properties.Settings.Default.DetailFontSize;
            if (title is not null)
                Title = title;
            TxtDetail.TextWrapping = wrapping;
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DetailFontSize = TxtDetail.FontSize = Math.Max(6, TxtDetail.FontSize - 2);
            Properties.Settings.Default.Save();
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DetailFontSize = TxtDetail.FontSize = Math.Min(30, TxtDetail.FontSize + 2);
            Properties.Settings.Default.Save();
        }
    }
}
