using System;
using System.Windows.Data;

namespace TwinCatVariableViewer
{

    [ValueConversion(typeof(object), typeof(int))]
    public class BoolToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            var _bool = (bool)System.Convert.ChangeType(value, typeof(bool));

            if (_bool)
                return 1;

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
