using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace TwinCatVariableViewer
{

    [ValueConversion(typeof(object), typeof(int))]
    public class BoolToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            bool _bool;
            try
            {

                _bool = (bool)System.Convert.ChangeType(value, typeof(bool));
            }
            catch (Exception)
            {
                _bool = false;
            }

            return _bool ? 1 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(object), typeof(string))]
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ListViewItem item = (ListViewItem)value;
            if (item != null)
            {
                ListView listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
                if (listView == null) return "";
                int index = listView.ItemContainerGenerator.IndexFromContainer(item);
                return index.ToString();
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
