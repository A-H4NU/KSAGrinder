using System;
using System.Windows;

namespace KSAGrinder.Windows
{
    /// <summary>
    /// Interaction logic for TradeDetail.xaml
    /// </summary>
    public partial class DetailView : Window
    {
        public DetailView(string detail, string title = null, TextWrapping wrapping = TextWrapping.NoWrap)
        {
            InitializeComponent();
            TxtDetail.Text = detail;
            if (title != null)
                Title = title;
            TxtDetail.TextWrapping = wrapping;
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            TxtDetail.FontSize = Math.Max(6, TxtDetail.FontSize - 2);
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            TxtDetail.FontSize = Math.Min(30, TxtDetail.FontSize + 2);
        }
    }
}
