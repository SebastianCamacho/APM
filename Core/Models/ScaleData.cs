using System;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa los datos de lectura de una báscula.
    /// Esta información se envía al POS Web.
    /// </summary>
    public class ScaleData
    {
        /// <summary>
        /// Tipo de evento, indicando que es una lectura de báscula (ej. "SCALE_READING").
        /// </summary>
        public string Type { get; set; } = "SCALE_READING";

        /// <summary>
        /// Identificador de la estación desde donde se realiza la lectura.
        /// </summary>
        public string StationId { get; set; }

        /// <summary>
        /// Identificador de la báscula que realizó la lectura.
        /// </summary>
        public string ScaleId { get; set; }

        /// <summary>
        /// El peso medido por la báscula.
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// Unidad de medida del peso (ej. "kg", "g", "lb").
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Indica si la lectura del peso es estable.
        /// </summary>
        public bool Stable { get; set; }

        /// <summary>
        /// Marca de tiempo (timestamp) de cuándo se realizó la lectura.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
