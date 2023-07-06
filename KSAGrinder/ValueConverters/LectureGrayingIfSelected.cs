using CommunityToolkit.Diagnostics;

using KSAGrinder.Components;
using KSAGrinder.Pages;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KSAGrinder.ValueConverters
{
    public class LectureGrayingIfSelected : IMultiValueConverter
    {
        private static MainPage? _mainPage;

        public static void Initialize(MainPage mainPage) => _mainPage = mainPage;

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            Guard.IsNotNull(_mainPage);
            bool isLightTheme = Properties.Settings.Default.Theme == "Light";

            if (value[0] is string code && value[1] is int grade)
            {
                foreach (Class @class in _mainPage.CurrentClassCollection)
                {
                    if (@class.Code == code && @class.Grade == grade)
                        return isLightTheme ? Brushes.LightGray : new SolidColorBrush(Color.FromRgb(98, 98, 98));
                }
            }
            return isLightTheme ? Brushes.Black : Brushes.White;
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}