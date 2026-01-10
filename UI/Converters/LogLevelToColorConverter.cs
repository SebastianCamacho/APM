using System;
using System.Globalization;
using AppsielPrintManager.Core.Interfaces; // Para LogLevel
using Microsoft.Maui.Controls;

namespace UI.Converters
{
    /// <summary>
    /// Convierte un LogLevel a un Color para mostrar mensajes de log con colores diferentes.
    /// También acepta un parámetro de cadena para comparar el nivel de log.
    /// </summary>
    public class LogLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                // Si el parámetro no es nulo y es una cadena, lo usamos para comparar.
                // Esto es para la condición OnPlatform que necesita un parámetro para comparar.
                if (parameter is string paramString && Enum.TryParse(paramString, out LogLevel paramLevel))
                {
                    return level == paramLevel;
                }

                // Si el targetType es Color, devolver el color correspondiente al nivel.
                if (targetType == typeof(Color))
                {
                    switch (level)
                    {
                        case LogLevel.Error:
                            return Colors.Red;
                        case LogLevel.Warning:
                            return Colors.Orange;
                        case LogLevel.Info:
                        default:
                            return Colors.Black; // Color por defecto para Info
                    }
                }
            }
            // Por defecto o si el tipo no es LogLevel, devolver el valor original o un default.
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
