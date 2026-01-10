// main.js - Script para probar la conexión WebSocket al APM

const websocketUrl = 'ws://localhost:7000/websocket/'; // La URL de tu servidor WebSocket APM
let socket;
const statusSpan = document.getElementById('status');
const messagesDiv = document.getElementById('messages');
let activePayloadId = 'payloadTicket'; // Por defecto

function logMessage(message, type = 'info') {
    const p = document.createElement('p');
    p.className = `log-${type}`;
    p.textContent = message;
    messagesDiv.appendChild(p);
    messagesDiv.scrollTop = messagesDiv.scrollHeight; // Scroll automático al final
}

function connectWebSocket() {
    // Si ya hay un socket y está abierto o en proceso de conexión, no intentar reconectar
    if (socket && (socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING)) {
        logMessage('Ya existe una conexión WebSocket activa o en proceso. No se intentará una nueva conexión.', 'warning');
        return;
    }

    statusSpan.textContent = 'Intentando conectar...';
    statusSpan.style.color = 'orange';
    logMessage('Intentando establecer conexión WebSocket con: ' + websocketUrl, 'info');

    try {
        socket = new WebSocket(websocketUrl);

        socket.onopen = (event) => {
            statusSpan.textContent = 'Conectado';
            statusSpan.style.color = 'green';
            logMessage('Conexión WebSocket establecida con el servidor APM.', 'success');
            console.log('WebSocket connection established:', event);
        };

        socket.onmessage = (event) => {
            logMessage('Mensaje recibido del servidor: ' + event.data);
            console.log('Message from server:', event.data);
        };

        socket.onclose = (event) => {
            statusSpan.textContent = 'Desconectado';
            statusSpan.style.color = 'red';
            let reason = event.reason || 'Conexión cerrada inesperadamente.';
            if (event.wasClean) {
                logMessage(`Conexión WebSocket cerrada limpiamente. Código: ${event.code}, Razón: ${reason}`, 'info');
            } else {
                logMessage(`Conexión WebSocket cerrada abruptamente. Código: ${event.code}, Razón: ${reason}`, 'error');
            }
            console.log('WebSocket connection closed:', event);
            socket = null; // Limpiar el objeto socket
        };

        socket.onerror = (error) => {
            statusSpan.textContent = 'Error';
            statusSpan.style.color = 'red';
            logMessage('Error en la conexión WebSocket. Consulta la consola del navegador para más detalles.', 'error');
            console.error('WebSocket Error:', error);
            // Intentar reconectar automáticamente en caso de error? Podríamos añadir un temporizador aquí.
        };

    } catch (error) {
        logMessage('Error al intentar crear el objeto WebSocket: ' + error.message, 'error');
        console.error('Error creating WebSocket object:', error);
    }
}

// Función para cerrar la conexión existente y luego intentar conectar de nuevo
function reconnectWebSocket() {
    if (socket) {
        if (socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING) {
            logMessage('Cerrando conexión WebSocket existente antes de reconectar...', 'info');
            socket.close(1000, 'Reconexión solicitada por el usuario'); // Código 1000 para cierre normal
        } else {
            logMessage('No hay conexión WebSocket abierta para cerrar, intentando conectar directamente.', 'info');
            connectWebSocket();
        }
    } else {
        connectWebSocket();
    }
}


// Función para enviar un mensaje
function sendTestMessage(message) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(message);
        logMessage('Mensaje enviado al servidor: ' + message, 'info');
    } else {
        logMessage('No hay conexión WebSocket abierta para enviar mensajes.', 'warning');
    }
}

// Plantillas JSON para cada tipo de documento
const templates = {
    ticket: {
        "JobId": "TICKET-001",
        "StationId": "CAJA_1",
        "PrinterId": "printHambuger",
        "DocumentType": "ticket_venta",
        "Data": {
            "company": { "Name": "Supermercado Demo", "Nit": "900123456", "Address": "Calle Principal #123" },
            "sale": { 
                "Number": "FV-1001", 
                "Date": new Date().toISOString(), 
                "Items": [
                    { "Name": "Leche Entera", "Qty": 2, "Total": 5000 },
                    { "Name": "Pan Tajado", "Qty": 1, "Total": 3500 }
                ],
                "Total": 8500
            },
            "footer": ["Gracias por su compra"]
        },
        "Images": [], "Barcodes": [], "QRs": []
    },
    comanda: {
        "JobId": "CMD-001",
        "StationId": "COCINA",
        "PrinterId": "printHambuger",
        "DocumentType": "comanda",
        "Data": {
            "order": { 
                "Number": "CMD-001",
                "Table": "Mesa 5", 
                "Waiter": "Carlos", 
                "Date": new Date().toISOString(),
                "Items": [
                    { "Name": "Hamburguesa Doble", "Qty": 1, "Notes": "Sin cebolla" },
                    { "Name": "Papas Fritas", "Qty": 1, "Notes": "Extra crocantes" },
                    { "Name": "Gaseosa", "Qty": 2 }
                ]
            }
        },
        "Images": [], "Barcodes": [], "QRs": []
    },
    factura: {
        "JobId": "FE-2025",
        "StationId": "ADMIN",
        "PrinterId": "printHambuger",
        "DocumentType": "factura_electronica",
        "Data": {
            "header": { "Title": "FACTURA ELECTRÓNICA DE VENTA", "Number": "FE-2025" },
            "customer": { "Name": "Empresa Cliente S.A.S", "Nit": "800.111.222-3", "Address": "Av. Empresarial 55" },
            "totals": { "Subtotal": 100000, "Tax": 19000, "Total": 119000 },
            "cufe": "abc1234567890def..."
        },
        "Images": [], "Barcodes": [], "QRs": []
    },
    barcodes: {
        "JobId": "BAR-001",
        "StationId": "BODEGA",
        "PrinterId": "printHambuger",
        "DocumentType": "codigos_barra",
        "Data": { "Label": "Etiqueta de Inventario" },
        "Images": [],
        "Barcodes": [
            { "Type": "CODE128", "Value": "PROD-12345", "Height": 60, "Hri": true, "Align": "center" }
        ],
        "QRs": []
    }
};

// Función global para cambiar de pestaña (llamada desde HTML)
window.openTab = function(tabName) {
    // Actualizar botones
    const buttons = document.querySelectorAll('.tab-button');
    buttons.forEach(btn => btn.classList.remove('active'));
    // Buscar el botón que llamó a la función (esto es un poco hacky si se llama programáticamente, pero funciona con onclick)
    const clickedBtn = Array.from(buttons).find(b => b.textContent.toLowerCase().includes(tabName.replace('ticket', 'ticket').replace('barcodes', 'códigos')));
    if(clickedBtn) clickedBtn.classList.add('active'); // Fallback simple
    
    // Encontrar el botón específico por texto o índice sería más robusto, pero para este script simple:
    // Vamos a confiar en que el usuario hace click. Para hacerlo visualmente correcto:
    if (event && event.target) {
        buttons.forEach(btn => btn.classList.remove('active'));
        event.target.classList.add('active');
    }

    // Actualizar Textareas
    const contents = document.querySelectorAll('.tab-content');
    contents.forEach(content => content.classList.remove('active'));
    
    const selectedId = 'payload' + tabName.charAt(0).toUpperCase() + tabName.slice(1);
    document.getElementById(selectedId).classList.add('active');
    activePayloadId = selectedId;
};

// Conectar automáticamente cuando la página se carga y adjuntar listeners al DOM
document.addEventListener('DOMContentLoaded', () => {
    connectWebSocket();

    // Inicializar los textareas con los JSONs
    document.getElementById('payloadTicket').value = JSON.stringify(templates.ticket, null, 4);
    document.getElementById('payloadComanda').value = JSON.stringify(templates.comanda, null, 4);
    document.getElementById('payloadFactura').value = JSON.stringify(templates.factura, null, 4);
    document.getElementById('payloadBarcodes').value = JSON.stringify(templates.barcodes, null, 4);

    const sendPayloadButton = document.getElementById('sendPayloadButton');
    const reconnectButton = document.getElementById('reconnectButton'); // Obtener el botón de reconexión

    if (sendPayloadButton) {
        sendPayloadButton.addEventListener('click', () => {
            const activeTextarea = document.getElementById(activePayloadId);
            if (activeTextarea) {
                const payload = activeTextarea.value;
                sendTestMessage(payload);
            } else {
                logMessage('Error: No se encontró el área de texto activa.', 'error');
            }
        });
    } else {
        logMessage('Error: No se encontraron los elementos HTML para enviar el payload.', 'error');
    }

    if (reconnectButton) {
        reconnectButton.addEventListener('click', reconnectWebSocket);
    } else {
        logMessage('Error: No se encontró el botón de reconexión.', 'error');
    }
});
