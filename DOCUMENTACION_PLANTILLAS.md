# Documentación del Sistema de Plantillas JSON - APM

Este documento describe todas las opciones disponibles para configurar las plantillas de impresión en **Appsiel Print Manager (APM)**. Las plantillas se guardan en formato JSON en la carpeta de datos de la aplicación.

---

## 1. Estructura de la Sección (`TemplateSection`)
Cada sección agrupa elementos y define el comportamiento lógico de un bloque del ticket.

| Propiedad | Tipo | Descripción |
| :--- | :--- | :--- |
| `Name` | `string` | Nombre identificador (ej: "Encabezado", "Items"). |
| `Type` | `string` | `"Static"` (secuencial) o `"Table"` (para listas con columnas). |
| `DataSource` | `string` | (Solo `Table`) Ruta a la colección de datos (ej: `"Sale.Items"`). |
| `Order` | `int` | Orden físico de impresión (menor a mayor). |
| `Align` | `string` | Alineación por defecto: `"Left"`, `"Center"`, `"Right"`. |
| `Format` | `string` | Formato de texto por defecto para toda la sección. |

---

## 2. Elementos (`TemplateElement`)
Componentes individuales dentro de una sección.

| Propiedad | Tipo | Descripción |
| :--- | :--- | :--- |
| `Type` | `string` | `"Text"`, `"Line"` (divisor), `"Barcode"`, `"QR"`, `"Image"`. |
| `Label` | `string` | Texto estático que precede al valor. |
| `Source` | `string` | Ruta dinámica al dato en el objeto JSON (ej: `"Sale.Total"`). |
| `StaticValue` | `string` | Valor fijo si no se especifica `Source`. |
| `WidthPercentage`| `int` | (Solo `Table`) % del ancho del papel que ocupa la columna. |
| `Align` | `string` | Alineación específica del elemento. |
| `Format` | `string` | Estilos aplicados (negrita, tamaño, fuente). |
| `HeaderLabel` | `string` | Texto para el encabezado de columna (si es tabla). |
| `HeaderAlign` | `string` | Alineación del texto del encabezado. |
| `HeaderFormat` | `string` | Formato del texto del encabezado. |

---

## 3. Formatos de Texto (`Format`)
Se pueden combinar múltiples valores separados por espacios (ej: `"Bold Large Center"`).

### Fuentes
*   `FontA`: Fuente estándar (48 caracteres en 80mm).
*   `FontB`: Fuente pequeña/comprimida (64 caracteres en 80mm).

### Estilos
*   `Bold`: Texto en negrita.
*   `Underline`: Texto subrayado.

### Tamaños
*   `Large`: Doble altura.
*   `DoubleWidth`: Doble ancho.
*   `SizeX`: Escala proporcional del 1 al 8 (ej: `Size2`, `Size3`).

---

## 4. Multimedia (Propiedades adicionales)

### Barcode (Tipo: `"Barcode"`)
*   Se configura mediante las propiedades del elemento.
*   Soporta `Height` (alto en puntos) y `Hri` (mostrar texto debajo).

### QR Code (Tipo: `"QR"`)
*   Soporta `Size` (tamaño del módulo del 1 al 16, recomendado: 3).

---

## 5. Ejemplo de Plantilla Completa
```json
{
  "DocumentType": "ticket_venta",
  "Name": "Plantilla Estándar",
  "Sections": [
    {
      "Name": "Header",
      "Type": "Static",
      "Align": "Center",
      "Elements": [
        { "Type": "Text", "StaticValue": "APPSIEL CLOUD POS", "Format": "Bold Size2" },
        { "Type": "Text", "Source": "Store.Address" },
        { "Type": "Line" }
      ]
    },
    {
      "Name": "Items",
      "Type": "Table",
      "DataSource": "Sale.Items",
      "Elements": [
        { "Label": "Cant", "Source": "Quantity", "WidthPercentage": 20 },
        { "Label": "Producto", "Source": "ProductName", "WidthPercentage": 50 },
        { "Label": "Total", "Source": "Total", "WidthPercentage": 30, "Align": "Right" }
      ]
    },
    {
      "Name": "Footer",
      "Type": "Static",
      "Align": "Center",
      "Elements": [
        { "Type": "Line" },
        { "Type": "Text", "Label": "TOTAL: ", "Source": "Sale.TotalAmount", "Format": "Bold Large" },
        { "Type": "QR", "Source": "Sale.InvoiceUrl", "Properties": { "Size": "4" } }
      ]
    }
  ]
}
```
