using System.Windows;

namespace KSAGrinder.Windows
{
    /// <summary>
    /// Interaction logic for TradeDetail.xaml
    /// </summary>
    public partial class DetailView : Window
    {
        public DetailView(string detail)
        {
            InitializeComponent();
            TxtDetail.Text = detail;
        }
    }
}
