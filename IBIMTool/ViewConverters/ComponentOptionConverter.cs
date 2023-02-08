using System;
using System.Windows;

namespace IBIMTool.ViewConverters
{
    internal class ComponentOptionConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(parameter is string parameterString))
            {
                return DependencyProperty.UnsetValue;
            }

            if (Enum.IsDefined(value.GetType(), value) == false)
            {
                return DependencyProperty.UnsetValue;
            }

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }


        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(parameter is string parameterString) ? DependencyProperty.UnsetValue : Enum.Parse(targetType, parameterString);
        }
    }
    public enum ComponentOption
    {
        OnlyVisible, SelectedElements, None
    }
}
