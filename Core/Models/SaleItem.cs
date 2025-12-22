namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa un ítem o producto individual dentro de una venta.
    /// </summary>
    public class SaleItem
    {
        /// <summary>
        /// Nombre del ítem o producto.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Cantidad del ítem o producto.
        /// </summary>
        public int Qty { get; set; }

        /// <summary>
        /// Precio unitario del ítem o producto.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Precio total para este ítem (cantidad * precio unitario).
        /// </summary>
        public decimal Total { get; set; }
    }
}
