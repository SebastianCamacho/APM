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
    // messagesDiv.scrollTop = messagesDiv.scrollHeight; // Scroll automático deshabilitado a petición
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

            try {
                const data = JSON.parse(event.data);
                if (data.Weight !== undefined && data.Unit !== undefined) {
                    // Is Scale Data
                    const display = document.getElementById('scaleDataDisplay');
                    if (display) {
                        display.textContent = `Báscula: ${data.ScaleId}\nPeso: ${data.Weight} ${data.Unit}\nEstable: ${data.Stable}\nTime: ${data.Timestamp}`;
                    }
                }
            } catch (e) {
                // Not JSON or other error
            }
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
        "Document": {
            "company": { "Name": "Supermercado Demo", "Nit": "900123456", "Address": "Calle Principal #123", "Phone": "555-1234" },
            "sale": { 
                "Number": "FV-1001", 
                "Date": new Date().toISOString(), 
                "Items": [
                    { "Name": "Leche Entera", "Qty": 2, "UnitPrice": 2500, "Total": 5000 },
                    { "Name": "Pan Tajado", "Qty": 1, "UnitPrice": 3500, "Total": 3500 }
                ],
                "Subtotal": 8500,
                "IVA": 0,
                "Total": 8500
            },
            "footer": ["Gracias por su compra"]
        }
    },
    comanda: {
        "JobId": "CMD-001",
        "StationId": "COCINA",
        "PrinterId": "printHambuger",
        "DocumentType": "comanda",
        "Document": {
            "order": { 
                "COPY":"ORIGINAL",
                "Number": "CMD-001",
                "Table": "Mesa 5", 
                "Waiter": "Carlos", 
                "Date": new Date().toISOString(),
                "RestaurantName": "Restaurant Tremendo Chuzo",
                "Items": [
                    { "Name": "Hamburguesa Doble", "Qty": 1, "Notes": "Sin cebolla" },
                    { "Name": "Papas Fritas", "Qty": 1, "Notes": "Extra crocantes" },
                    { "Name": "Gaseosa", "Qty": 2, "Notes": "" }
                ],
                "GeneratedDate": new Date().toISOString(),
                "CreatedBy": "Jose Reyes"
            },
            "Detail": "Todoterreno sin piña, full arepa sin maíz y la salchipapa sin lechuga" // Detalle global
        }
    },
    factura: {
        "JobId": "FE-2025",
        "StationId": "ADMIN",
        "PrinterId": "printHambuger",
        "DocumentType": "factura_electronica",
        "Document": {
            "Seller": {
                "Name": "Appsiel S.A.S",
                "Nit": "900.123.456-7",
                "TaxRegime": "Responsable de IVA",
                "Address": "Calle 123 #45-67",
                "City": "Bogotá, Colombia",
                "Phone": "300 123 4567",
                "Email": "facturacion@appsiel.com.co",
                "ResolutionNumber": "18760000001",
                "ResolutionDate": "2024-01-01T00:00:00",
                "ResolutionPrefix": "FE",
                "ResolutionFrom": "1",
                "ResolutionTo": "10000",
                "ResolutionText": "Autorización de Numeración de Facturación Electrónica No. 18760000001 del 2024-01-01 al 2025-01-01 del FE-1 al FE-10000"
            },
            "Buyer": {
                "Name": "Cliente VIP Ltda",
                "Nit": "800.987.654-3",
                "Address": "Carrera 7 #80-20, Bogotá",
                "Email": "facturacion@clientevip.com"
            },
            "Invoice": {
                "Number": "FE-9876",
                "IssueDate": new Date().toISOString(),
                "DueDate": new Date(new Date().getTime() + 30*24*60*60*1000).toISOString(), // +30 días
                "PaymentMethod": "Crédito 30 Días",
                "PaymentMeans": "Transferencia Bancaria",
                "Currency": "COP",
                "Items": [
                    { 
                        "Code": "SERV-001", 
                        "Description": "Servicio de Consultoría Software", 
                        "Quantity": 10, 
                        "UnitPrice": 150000, 
                        "Discount": 0, 
                        "IvaRate": 19, 
                        "IvaAmount": 285000, 
                        "Total": 1785000 
                    },
                    { 
                        "Code": "LIC-005", 
                        "Description": "Licencia Anual Appsiel Pro", 
                        "Quantity": 1, 
                        "UnitPrice": 2500000, 
                        "Discount": 250000, // 10%
                        "IvaRate": 19, 
                        "IvaAmount": 427500, 
                        "Total": 2677500 
                    }
                ],
                "Subtotal": 3750000,
                "Discount": 250000,
                "Iva": 712500,
                "Total": 4462500,
                "Taxes": [
                    { "Name": "IVA 19%", "Base": 3750000, "Rate": 19, "Amount": 712500 }
                ]
            },
            "TechKey": "de30fc14561023a134...", // CUFE real suele ser muy largo
            "QrString": "https://www.appsiel.com.co/",
            "LegalNotes": [
                "Esta factura se asimila en todos sus efectos a una letra de cambio (Art. 774 C.Co)",
                "Régimen Común - Iva Responsable"
            ]
        }
    },
    sticker: { // Nuevo tipo de documento
        "JobId": "STICKER-001",
        "StationId": "ALMACEN",
        "PrinterId": "printHambuger", // Usar una impresora configurada
        "DocumentType": "sticker_codigo_barras",
        "Document": {
            "stickers": [
                {
                    "type": "CODE128",
                    "item_id": "170",
                    "name": "BLUSA VERONICA XS",
                    "price": "$90.000",
                    "height": 80,
                    "hri": true,
                    "value": "123456" 
                },
                {
                    "type": "CODE128",
                    "item_id": "171",
                    "name": "PANTALON JEAN 30",
                    "price": "$120.000",
                    "height": 80,
                    "hri": true,
                    "value": "654321" 
                }
            ]
        }
    }
};

// Función global para cambiar de pestaña (llamada desde HTML)
window.openTab = function(tabName) {
    // Actualizar botones
    const buttons = document.querySelectorAll('.tab-button');
    buttons.forEach(btn => btn.classList.remove('active'));
    // Encontrar el botón específico por texto o índice
    const clickedBtn = Array.from(buttons).find(b => b.dataset.tab === tabName);
    if(clickedBtn) clickedBtn.classList.add('active');
    
    // Actualizar Textareas
    const contents = document.querySelectorAll('.tab-content');
    contents.forEach(content => content.classList.remove('active'));
    
    const selectedId = 'payload' + tabName.charAt(0).toUpperCase() + tabName.slice(1);
    document.getElementById(selectedId).classList.add('active');
    activePayloadId = selectedId;
};

// Conectar automáticamente cuando la página se carga y adjuntar listeners al DOM
document.addEventListener('DOMContentLoaded', () => {
    console.log("DOM Content Loaded. Initializing WebSocket client script."); // Added for debugging
    connectWebSocket();

    // Inicializar los textareas con los JSONs
    document.getElementById('payloadTicket').value = JSON.stringify(templates.ticket, null, 4);
    document.getElementById('payloadComanda').value = JSON.stringify(templates.comanda, null, 4);
    document.getElementById('payloadFactura').value = JSON.stringify(templates.factura, null, 4);
    document.getElementById('payloadSticker').value = JSON.stringify(templates.sticker, null, 4); // Nuevo

    // Attach event listeners for tab buttons
    const tabButtons = document.querySelectorAll('.tab-button');
    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            window.openTab(button.dataset.tab);
        });
    });
    const sendPayloadButton = document.getElementById('sendPayloadButton');
    const reconnectButton = document.getElementById('reconnectButton'); // Obtener el botón de reconexión

    if (sendPayloadButton) {
        console.log("sendPayloadButton found."); // Added for debugging
        sendPayloadButton.addEventListener('click', (event) => {
            event.preventDefault();
            console.log("Send button clicked. Active payload ID:", activePayloadId);
            const activeTextarea = document.getElementById(activePayloadId);
            if (activeTextarea) {
                const payload = activeTextarea.value;
                sendTestMessage(payload);
            } else {
                logMessage('Error: No se encontró el área de texto activa.', 'error');
            }
        });
    } else {
        console.error('CRITICAL ERROR: Element with ID "sendPayloadButton" not found.'); // Changed to console.error
        logMessage('Error: No se encontraron los elementos HTML para enviar el payload.', 'error');
    }

    if (reconnectButton) {
        console.log("reconnectButton found."); // Added for debugging
        reconnectButton.addEventListener('click', reconnectWebSocket);
    } else {
        console.error('CRITICAL ERROR: Element with ID "reconnectButton" not found.'); // Changed to console.error
        logMessage('Error: No se encontró el botón de reconexión.', 'error');
    }

    // Activar la primera pestaña por defecto
    window.openTab('ticket'); // Abre la pestaña de Ticket por defecto


    // --- Scale Test Logic ---
    const toggleListeningButton = document.getElementById('toggleListeningButton');
    const scaleIdInput = document.getElementById('scaleIdInput');
    const scaleDataDisplay = document.getElementById('scaleDataDisplay');
    let isListeningToScale = false;

    if (toggleListeningButton && scaleIdInput && scaleDataDisplay) {
        toggleListeningButton.addEventListener('click', () => {
             if (!isListeningToScale) {
                 // Start Listening
                 const scaleId = scaleIdInput.value;
                 if (!scaleId) {
                     alert("Por favor ingrese un ID de báscula");
                     return;
                 }
                 
                 const command = JSON.stringify({
                     Action: "StartListening",
                     ScaleId: scaleId
                 });
                 
                 sendTestMessage(command);
                 
                 // Update UI
                 isListeningToScale = true;
                 toggleListeningButton.textContent = "Dejar de Escuchar";
                 toggleListeningButton.style.backgroundColor = "#d9534f"; // Red
                 scaleDataDisplay.textContent = "Escuchando... Esperando datos...";
             } else {
                 // Stop Listening
                 const command = JSON.stringify({
                     Action: "StopListening"
                 });
                 
                 sendTestMessage(command);
                 
                 // Update UI
                 isListeningToScale = false;
                 toggleListeningButton.textContent = "Empezar a Escuchar";
                 toggleListeningButton.style.backgroundColor = "#4CAF50"; // Green
                 scaleDataDisplay.textContent += "\nStopped listening.";
             }
        });
    }
});
