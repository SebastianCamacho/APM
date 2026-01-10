using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace UI.Converters
{
    /// <summary>
    /// Convierte valores numéricos (int, decimal, etc.) a string y viceversa para el TwoWay Binding en Entries.
    /// Maneja valores nulos y parseo seguro.
    /// </summary>
    public class NumericToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            // Intenta convertir el número a string
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string stringValue || string.IsNullOrWhiteSpace(stringValue))
            {
                // Si el valor es nulo, vacío o no string, devuelve el valor predeterminado para el tipo de destino o null.
                // Esto es crucial para evitar errores si el usuario borra el contenido de un Entry.
                if (targetType == typeof(int) || targetType == typeof(decimal))
                    return 0; // O un valor que indique "vacío" o "no válido" para su lógica
                return null;
            }

            // Intenta parsear a int
            if (targetType == typeof(int) && int.TryParse(stringValue, out int intResult))
            {
                return intResult;
            }

            // Intenta parsear a decimal
            if (targetType == typeof(decimal) && decimal.TryParse(stringValue, NumberStyles.Any, culture, out decimal decimalResult))
            {
                return decimalResult;
            }
            
            // Si no se puede convertir al tipo de destino, devuelve el valor predeterminado del tipo o lanza una excepción.
            // Para simplificar, devolvemos 0 para int/decimal si el parseo falla.
            if (targetType == typeof(int)) return 0;
            if (targetType == typeof(decimal)) return 0m;

            return null; // O throw new ArgumentException("Cannot convert value to target type.");
        }
    }
}
