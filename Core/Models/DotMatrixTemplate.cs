using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa una plantilla específica para impresoras matriciales
    /// basada en coordenadas fijas (Fila, Columna).
    /// </summary>
    public class DotMatrixTemplate
    {
        public string? TemplateId { get; set; }
        public string? Name { get; set; }
        public string? DocumentType { get; set; }

        /// <summary>
        /// Número total de filas del documento pre-impreso (ej: 66 para tamaño carta).
        /// </summary>
        public int TotalRows { get; set; } = 66;

        /// <summary>
        /// Número máximo de columnas (ej: 80 para normal, 137 para comprimido).
        /// </summary>
        public int TotalColumns { get; set; } = 80;

        public List<DotMatrixElement> Elements { get; set; } = new List<DotMatrixElement>();
    }

    /// <summary>
    /// Define un campo de texto en una posición exacta.
    /// </summary>
    public class DotMatrixElement
    {
        public string? Label { get; set; }
        public string? Source { get; set; }
        public string? StaticValue { get; set; }

        /// <summary>
        /// Fila 1-indexada.
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// Columna 1-indexada.
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Longitud máxima permitida para este campo.
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// Formato opcional (Condensed, Bold).
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Para listas repetidas (como items de factura), indica la fuente de la lista.
        /// </summary>
        public string? DataSource { get; set; }

        /// <summary>
        /// Espaciado entre filas para secciones repetidas.
        /// </summary>
        public int? RowIncrement { get; set; }

        /// <summary>
        /// Carácter de relleno (ej: '*')
        /// </summary>
        public char? PaddingChar { get; set; }

        /// <summary>
        /// Tipo de relleno ("Left" o "Right")
        /// </summary>
        public string? PaddingType { get; set; }

        /// <summary>
        /// Fila a la que salta el texto si se desborda.
        /// </summary>
        public int? WrapToRow { get; set; }

        /// <summary>
        /// Columna a la que salta el texto si se desborda.
        /// </summary>
        public int? WrapToColumn { get; set; }

        /// <summary>
        /// Longitud máxima permitida en la segunda línea.
        /// </summary>
        public int? WrapMaxLength { get; set; }
    }
}
