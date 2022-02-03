
using KSAGrinder.Components;
using KSAGrinder.Pages;

using System;
using System.Windows;

namespace KSAGrinder.Windows
{
    /// <summary>
    /// TradeFinder.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TradeFinder : Window
    {
        private readonly TradeFinderMain _mainPage;

        public TradeFinder(string studentId, Schedule schedule)
        {
            InitializeComponent();

            Main.Content = _mainPage = new TradeFinderMain(this, studentId, schedule);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _mainPage.StopWorking();
        }
    }
}