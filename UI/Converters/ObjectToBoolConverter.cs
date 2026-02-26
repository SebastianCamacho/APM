using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace UI.Converters
{
    public class ObjectToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter?.ToString() == "NOTNULL")
            {
                return value != null;
            }

            if (value is bool b) return b;
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
