using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace KSAGrinder.Statics
{
    public class ColorFromTheme : DependencyObject
    {
        private struct Theme
        {
            public Brush Background { get; private set; }
            public Brush Font { get; private set; }
            public Brush Header { get; private set; }
            public Brush Button { get; private set; }

            public Theme(Color background, Color font, Color header, Color button)
            {
                Background = new SolidColorBrush(background);
                Font = new SolidColorBrush(font);
                Header = new SolidColorBrush(header);
                Button = new SolidColorBrush(button);
            }
        }

        private static readonly Dictionary<string, Theme> _themes = new Dictionary<string, Theme>();

        static ColorFromTheme()
        {
            Theme light = new Theme(Colors.White, Colors.Black, Colors.LightGray, Colors.LightGray);
            _themes.Add("Light", light);

            Theme black = new Theme(Colors.Black, Colors.White, Colors.DarkGray, Colors.DarkGray);
            _themes.Add("Black", black);

            Theme dark = new Theme(
                Color.FromRgb(0x2e, 0x2e, 0x2e),
                Colors.White,
                Color.FromRgb(0x4d, 0x4d, 0x4d),
                Color.FromRgb(0x4d, 0x4d, 0x4d));
            _themes.Add("Dark", dark);
        }

        public static IEnumerable<string> ThemeNames => _themes.Keys;

        private static Theme GetCurrentTheme() => _themes[Properties.Settings.Default.Theme];



        public static Brush GetBackgroundColor(DependencyObject obj)
        {
            return GetCurrentTheme().Background;
            //return (Brush)obj.GetValue(BackgroundColorProperty);
        }

        public static void SetBackgroundColor(DependencyObject obj, Brush value)
        {
            throw new NotImplementedException();
            //obj.SetValue(BackgroundColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for BackgroundColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.RegisterAttached("BackgroundColor", typeof(Brush), typeof(ColorFromTheme), new PropertyMetadata(0));

        public static Brush FontColor => GetCurrentTheme().Font;

        public static Brush HeaderColor => GetCurrentTheme().Header;

        public static Brush ButtonColor => GetCurrentTheme().Button;
    }
}
