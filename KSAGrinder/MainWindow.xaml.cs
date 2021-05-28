using KSAGrinder.Pages;

using System.Windows;

namespace KSAGrinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Main.Content = new FileInput(this);
        }
    }
}
