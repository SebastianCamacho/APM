using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace UI.Converters
{
    public class BoolToImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolVal && parameter is string paramString)
            {
                var keys = paramString.Split('|');
                if (keys.Length == 2)
                {
                    return boolVal ? keys[0] : keys[1];
                }
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
