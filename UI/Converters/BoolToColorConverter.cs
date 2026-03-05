using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace UI.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string[] colors)
            {
                var trueColorKey = colors.Length > 0 ? colors[0] : "";
                var falseColorKey = colors.Length > 1 ? colors[1] : "";

                var key = boolValue ? trueColorKey : falseColorKey;

                if (Application.Current != null && Application.Current.Resources.TryGetValue(key, out var color))
                    return color;
            }
            else if (value is bool boolVal && parameter is string paramString)
            {
                var keys = paramString.Split('|');
                if (keys.Length == 2)
                {
                    var key = boolVal ? keys[0] : keys[1];

                    if (Application.Current != null && Application.Current.Resources.TryGetValue(key, out var appColor))
                        return appColor;

                    // Support colors defined on the page (MAUI Multi-Window Support)
                    if (Application.Current != null)
                    {
                        var window = Application.Current.Windows.FirstOrDefault();
                        if (window != null && window.Page != null)
                        {
                            if (window.Page.Resources.TryGetValue(key, out var pageColor))
                                return pageColor;

                            if (window.Page is NavigationPage nav && nav.CurrentPage != null)
                            {
                                if (nav.CurrentPage.Resources.TryGetValue(key, out var currentColor))
                                    return currentColor;
                            }
                        }
                    }
                }
            }
            return Colors.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
