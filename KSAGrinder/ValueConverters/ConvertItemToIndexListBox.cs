using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace KSAGrinder.ValueConverters
{
    public class ConvertItemToIndexListBox : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            ListBoxItem item = (ListBoxItem)value;
            ListBox listView = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
            int index = listView.ItemContainerGenerator.IndexFromContainer(item);
            return String.Format("{0:0000}", index + 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
