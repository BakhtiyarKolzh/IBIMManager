using System;
using System.Globalization;
using System.Windows.Data;


namespace IBIMTool.ViewConverters
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double size = System.Convert.ToDouble(value);
            if (parameter is string percent)
            {
                if (double.TryParse(percent, out double result))
                {
                    return size * result;
                }
            }
            return size;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}