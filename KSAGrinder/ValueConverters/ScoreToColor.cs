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
            Color light = Color.FromRgb(105, 173, 105);
            Color dark = Color.FromRgb(35, 75, 35);
            if (value is double score)
            {
                Color newColor = Color.FromRgb(
                    (byte)((light.R * score + dark.R * (100 - score)) / 100),
                    (byte)((light.G * score + dark.G * (100 - score)) / 100),
                    (byte)((light.B * score + dark.B * (100 - score)) / 100));
                return new SolidColorBrush(newColor);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
