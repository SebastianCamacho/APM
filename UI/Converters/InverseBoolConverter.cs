using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace UI.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value; // Devuelve el valor original si no es un booleano
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value; // Devuelve el valor original si no es un booleano
        }
    }
}
