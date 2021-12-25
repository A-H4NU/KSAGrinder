// Credit: https://stackoverflow.com/questions/6367876/how-disable-navigation-shortcuts-in-frame-c-sharp-wpf

using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace KSAGrinder.Statics
{
    public static class DisableNavigation
    {
        public static bool GetDisable(DependencyObject o)
        {
            return (bool)o.GetValue(DisableProperty);
        }

        public static void SetDisable(DependencyObject o, bool value)
        {
            o.SetValue(DisableProperty, value);
        }

        public static readonly DependencyProperty DisableProperty =
            DependencyProperty.RegisterAttached("Disable", typeof(bool), typeof(DisableNavigation),
                                                new PropertyMetadata(false, DisableChanged));

        public static void DisableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Frame frame = (Frame)sender;
            frame.Navigated += DontNavigate;
            frame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
        }

        public static void DontNavigate(object sender, NavigationEventArgs e)
        {
            ((Frame)sender).NavigationService.RemoveBackEntry();
        }
    }
}
