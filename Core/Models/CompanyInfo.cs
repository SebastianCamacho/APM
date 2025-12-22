using System;
using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa la información de la compañía en un trabajo de impresión.
    /// </summary>
    public class CompanyInfo
    {
        /// <summary>
        /// Nombre de la compañía.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Número de Identificación Tributaria (NIT) de la compañía.
        /// </summary>
        public string Nit { get; set; }

        /// <summary>
        /// Dirección de la compañía.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Número de teléfono de la compañía.
        /// </summary>
        public string Phone { get; set; }
    }
}
