using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Convert bool binding to int (0 or 1)
    /// </summary>
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

    /// <summary>
    /// Convert bool binding to visibility: true -> visible, false -> hidden
    /// </summary>
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class ValueToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Hidden;
            }

            var boolValue = System.Convert.ToBoolean(value);
            if (parameter?.ToString() == "Invert") boolValue = !boolValue;

            if (boolValue)
            {
                return Visibility.Visible;
            }

            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// Convert ListView item binding to item index int as string 
    /// </summary>
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
