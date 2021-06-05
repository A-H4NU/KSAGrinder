using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KSAGrinder.ValueConverters
{
    public class ScoreToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color green = Colors.LightGreen;
            Color red = Colors.Coral;
            if (value is double score)
            {
                var newColor = Color.FromRgb(
                    (byte)(green.R * score + red.R * (1 - score)),
                    (byte)(green.G * score + red.G * (1 - score)),
                    (byte)(green.B * score + red.B * (1 - score)));
                return new SolidColorBrush(newColor);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
