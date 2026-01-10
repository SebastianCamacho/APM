using System;
using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa la información detallada de una comanda de cocina/bar para un trabajo de impresión.
    /// </summary>
    public class CommandInfo
    {
        /// <summary>
        /// Número del pedido o comanda.
        /// </summary>
        public string OrderNumber { get; set; }

        /// <summary>
        /// Fecha de la comanda.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Cliente o mesa asociada a la comanda.
        /// </summary>
        public string Client { get; set; }

        /// <summary>
        /// Persona que atendió el pedido.
        /// </summary>
        public string AttendedBy { get; set; }

        /// <summary>
        /// Lista de ítems o productos incluidos en la comanda.
        /// </summary>
        public List<CommandItem> Items { get; set; } = new List<CommandItem>();

        /// <summary>
        /// Detalles o notas adicionales sobre la comanda.
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// Nombre del restaurante o lugar.
        /// </summary>
        public string RestaurantName { get; set; }

        /// <summary>
        /// Fecha y hora en que se generó la comanda.
        /// </summary>
        public DateTime GeneratedDate { get; set; }

        /// <summary>
        /// Nombre de quien creó la comanda.
        /// </summary>
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// Representa un ítem o producto individual dentro de una comanda.
    /// </summary>
    public class CommandItem
    {
        /// <summary>
        /// Nombre del ítem o producto.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Cantidad del ítem o producto.
        /// </summary>
        public int Qty { get; set; }
    }
}
