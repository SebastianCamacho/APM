// main.js - Script para probar la conexión WebSocket al APM

const websocketUrl = 'ws://localhost:7000/websocket/'; // La URL de tu servidor WebSocket APM
let socket;
const statusSpan = document.getElementById('status');
const messagesDiv = document.getElementById('messages');
let jsonPayloadTextarea; 

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

// Conectar automáticamente cuando la página se carga y adjuntar listeners al DOM
document.addEventListener('DOMContentLoaded', () => {
    connectWebSocket();

    jsonPayloadTextarea = document.getElementById('jsonPayload');
    const sendPayloadButton = document.getElementById('sendPayloadButton');
    const reconnectButton = document.getElementById('reconnectButton'); // Obtener el botón de reconexión

    if (sendPayloadButton && jsonPayloadTextarea) {
        sendPayloadButton.addEventListener('click', () => {
            const payload = jsonPayloadTextarea.value;
            sendTestMessage(payload);
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
