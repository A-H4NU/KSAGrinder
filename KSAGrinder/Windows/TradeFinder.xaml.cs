using KoreanText;

using KSAGrinder.Components;
using KSAGrinder.Exceptions;
using KSAGrinder.Extensions;
using KSAGrinder.Pages;
using KSAGrinder.Statics;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KSAGrinder.Windows
{
    /// <summary>
    /// TradeFinder.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TradeFinder : Window
    {
        public TradeFinder(string studentId, Schedule schedule)
        {
            InitializeComponent();

            Main.Content = new TradeFinderMain(this, studentId, schedule);
        }
    }
}