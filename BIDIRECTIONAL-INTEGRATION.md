# 🔄 Integración Bidireccional AION ↔️ Claude Code

## 📋 Descripción

Este documento describe el **sistema bidireccional completo** que permite:
- 🎤 **AION → Claude**: AION puede llamar a Claude Code para asistencia AI
- 🎮 **Claude → AION**: Claude Code puede controlar AION Desktop Assistant

---

## 🏗️ Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│                    🔄 SISTEMA BIDIRECCIONAL                   │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────┐         ┌──────────────────┐          │
│  │  🤖 Claude Code  │ ◄─────► │  💻 AION Desktop │          │
│  │      CLI         │         │    Assistant     │          │
│  └──────────────────┘         └──────────────────┘          │
│           │                             │                    │
│           │ HTTP REST API               │ Voice/Automation   │
│           │ Port 8080                   │ Services           │
│           │                             │                    │
│           ▼                             ▼                    │
│  ┌──────────────────────────────────────────────┐           │
│  │       AionRemoteControlService               │           │
│  │  • HTTP Server (localhost:8080)              │           │
│  │  • 14 API Endpoints                          │           │
│  │  • Full automation control                   │           │
│  │  • Screen reading & OCR                      │           │
│  │  • Voice synthesis & recognition             │           │
│  └──────────────────────────────────────────────┘           │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎯 Dirección 1: AION → Claude (Ya implementado v1.1.0)

### ✨ Características
- ✅ `ClaudeCodeIntegrationService`
- ✅ Comandos de voz para invocar a Claude
- ✅ Análisis de pantalla con contexto
- ✅ Sugerencias de accesibilidad AI

### 🎤 Comandos de Voz
```
"Ask Claude [pregunta]"
"Claude help"
"Analyze screen with Claude"
"Claude accessibility"
```

---

## 🎮 Dirección 2: Claude → AION (NUEVO!)

### 🌟 Servidor HTTP REST API

**Puerto**: `http://localhost:8080`
**Protocolo**: HTTP/REST
**Formato**: JSON
**CORS**: Habilitado para Claude Code CLI

### 📡 API Endpoints (14 disponibles)

#### 1. 📊 Estado del Sistema
```http
GET /api/status
```
**Response:**
```json
{
  "Success": true,
  "Message": "AION Desktop Assistant is running",
  "Data": {
    "Status": "Online",
    "RequestsHandled": 42,
    "VoiceListening": true,
    "ServerUptime": "00:15:30"
  }
}
```

#### 2. 📸 Captura de Pantalla
```http
GET /api/screen/capture
```
**Response:**
```json
{
  "Success": true,
  "Message": "Screenshot captured",
  "Data": {
    "Image": "base64_encoded_image...",
    "Width": 1920,
    "Height": 1080
  }
}
```

#### 3. 👁️ Lectura de Pantalla con OCR
```http
GET /api/screen/read
```
**Response:**
```json
{
  "Success": true,
  "Message": "Screen text extracted",
  "Data": {
    "Text": "Texto extraído de la pantalla...",
    "Length": 1234
  }
}
```

#### 4. 🖱️ Mover Mouse
```http
POST /api/mouse/move
Content-Type: application/json

{
  "X": 500,
  "Y": 300
}
```

#### 5. 🖱️ Click del Mouse
```http
POST /api/mouse/click
Content-Type: application/json

{
  "X": 500,     // Opcional - si no se provee, click en posición actual
  "Y": 300,     // Opcional
  "Button": "Left"  // Opcional: Left, Right, Middle
}
```

#### 6. ⌨️ Escribir Texto
```http
POST /api/keyboard/type
Content-Type: application/json

{
  "Text": "Hello from Claude!"
}
```

#### 7. ⌨️ Presionar Tecla
```http
POST /api/keyboard/press
Content-Type: application/json

{
  "Key": "Enter"  // Enter, Tab, Escape, etc.
}
```

#### 8. 🪟 Listar Ventanas
```http
GET /api/window/list
```
**Response:**
```json
{
  "Success": true,
  "Message": "Found 5 windows",
  "Data": {
    "Windows": ["Chrome", "Visual Studio Code", "Discord", "Notepad", "Explorer"],
    "Count": 5
  }
}
```

#### 9. 🪟 Cambiar Ventana
```http
POST /api/window/switch
Content-Type: application/json

{
  "WindowName": "Chrome"
}
```

#### 10. 🔊 Hablar (Text-to-Speech)
```http
POST /api/voice/speak
Content-Type: application/json

{
  "Text": "Hello, I am Claude controlling AION"
}
```

#### 11. 🎤 Activar Escucha de Voz
```http
POST /api/voice/listen
```

#### 12. ♿ Análisis de Accesibilidad
```http
GET /api/accessibility/analyze
```

#### 13. 🎯 Ejecutar Comando Natural
```http
POST /api/command/execute
Content-Type: application/json

{
  "Command": "read screen and tell me what you see"
}
```

---

## 🖥️ Cliente CLI: claude-aion-cli.js

### 📦 Instalación
```bash
# Ya incluido en el proyecto AION
cd D:\AION-Desktop-Assistant
node claude-aion-cli.js help
```

### 🚀 Uso desde Claude Code

#### Comandos Básicos
```bash
# Verificar estado de AION
node claude-aion-cli.js status

# Leer pantalla
node claude-aion-cli.js read-screen

# Capturar screenshot
node claude-aion-cli.js capture-screen
```

#### Control de Mouse
```bash
# Mover mouse
node claude-aion-cli.js move-mouse 500 300

# Click en posición actual
node claude-aion-cli.js click

# Click en coordenadas específicas
node claude-aion-cli.js click 500 300
```

#### Control de Teclado
```bash
# Escribir texto
node claude-aion-cli.js type "Hello World"

# Presionar tecla
node claude-aion-cli.js press Enter
node claude-aion-cli.js press Tab
```

#### Gestión de Ventanas
```bash
# Listar ventanas
node claude-aion-cli.js list-windows

# Cambiar a ventana específica
node claude-aion-cli.js switch-window "Chrome"
```

#### Voz y Audio
```bash
# Hablar
node claude-aion-cli.js speak "I am Claude and I'm controlling this computer"

# Activar reconocimiento de voz
node claude-aion-cli.js listen
```

#### Análisis
```bash
# Análisis de accesibilidad
node claude-aion-cli.js analyze

# Comando en lenguaje natural
node claude-aion-cli.js execute "read the screen and click on the submit button"
```

---

## 🔄 Flujo de Trabajo Bidireccional

### Escenario 1: Usuario pide ayuda a Claude
```
1. Usuario (voz) → "Ask Claude, how do I close this window?"
2. AION → Captura pantalla + extrae texto
3. AION → Envía a Claude Code CLI
4. Claude → Procesa y responde
5. AION → Habla respuesta al usuario
```

### Escenario 2: Claude controla AION
```
1. Claude Code CLI → POST /api/screen/read
2. AION → Responde con texto OCR
3. Claude → Analiza y decide acción
4. Claude → POST /api/mouse/move + /api/mouse/click
5. AION → Ejecuta acciones
6. Claude → POST /api/voice/speak "Task completed"
7. AION → Habla confirmación
```

### Escenario 3: Loop interactivo completo
```
1. Claude → GET /api/screen/read (lee pantalla)
2. Claude → Analiza contenido
3. Claude → POST /api/command/execute "click on settings"
4. AION → Ejecuta y confirma
5. Claude → GET /api/screen/read (verifica resultado)
6. Claude → POST /api/voice/speak "Settings opened successfully"
7. Usuario escucha confirmación
```

---

## 💻 Ejemplo de Uso desde Claude Code

### Script Python para Claude
```python
import requests
import json

AION_API = "http://localhost:8080"

# 1. Verificar que AION está activo
response = requests.get(f"{AION_API}/api/status")
print(response.json())

# 2. Leer pantalla
response = requests.get(f"{AION_API}/api/screen/read")
screen_text = response.json()["Data"]["Text"]
print(f"Screen text: {screen_text}")

# 3. Mover mouse y hacer click
requests.post(f"{AION_API}/api/mouse/move",
    json={"X": 500, "Y": 300})
requests.post(f"{AION_API}/api/mouse/click")

# 4. Escribir texto
requests.post(f"{AION_API}/api/keyboard/type",
    json={"Text": "Hello from Claude!"})

# 5. Hablar
requests.post(f"{AION_API}/api/voice/speak",
    json={"Text": "Task completed successfully"})
```

### Script Node.js para Claude
```javascript
const http = require('http');

async function callAion(endpoint, method = 'GET', body = null) {
    // Ver implementación completa en claude-aion-cli.js
}

// Uso
await callAion('/api/screen/read');
await callAion('/api/mouse/click', 'POST', { X: 500, Y: 300 });
await callAion('/api/keyboard/type', 'POST', { Text: 'Hello!' });
```

---

## 🔒 Seguridad

### ✅ Medidas Implementadas
- 🔐 **Localhost Only**: El servidor solo escucha en `localhost:8080`
- 🚫 **Sin autenticación externa**: Protegido por firewall local
- ✅ **CORS limitado**: Solo permite origin específicos
- 📊 **Logging completo**: Todas las requests se registran
- 🛡️ **Error handling**: Manejo robusto de errores

### ⚠️ Consideraciones
- El servidor HTTP solo debe ejecutarse en entorno local confiable
- No exponer el puerto 8080 públicamente
- Usar VPN/SSH tunnel para acceso remoto si es necesario
- Revisar logs regularmente: `logs/aion-desktop-assistant-*.log`

---

## 🧪 Testing

### Test Manual desde Terminal
```bash
# Windows PowerShell
Invoke-WebRequest -Uri http://localhost:8080/api/status -Method GET

# Linux/Mac
curl http://localhost:8080/api/status

# Con body JSON
curl -X POST http://localhost:8080/api/mouse/move \
  -H "Content-Type: application/json" \
  -d '{"X": 500, "Y": 300}'
```

### Test desde Node.js
```bash
node claude-aion-cli.js status
node claude-aion-cli.js read-screen
node claude-aion-cli.js move-mouse 100 200
```

### Test desde Python
```python
import requests
r = requests.get("http://localhost:8080/api/status")
print(r.json())
```

---

## 📊 Monitoreo y Logs

### Ver Logs en Tiempo Real
```powershell
# Windows
Get-Content -Path "logs\aion-desktop-assistant-*.log" -Wait

# Linux/Mac
tail -f logs/aion-desktop-assistant-*.log
```

### Formato de Logs
```
[2025-10-04 15:30:45.123 -06:00 INF] 🔄 Remote Control Server started
[2025-10-04 15:30:50.456 -06:00 DBG] 📥 Incoming request #1: GET /api/status
[2025-10-04 15:30:50.789 -06:00 INF] ✅ Request #1 handled in 23ms - OK
[2025-10-04 15:31:00.123 -06:00 DBG] 📥 Incoming request #2: POST /api/mouse/move
[2025-10-04 15:31:00.456 -06:00 INF] ✅ Request #2 handled in 15ms - OK
```

---

## 🎯 Casos de Uso

### 1. Claude como Asistente Proactivo
```
Claude detecta que el usuario necesita ayuda
→ Lee la pantalla con OCR
→ Identifica el problema
→ Ejecuta la solución automáticamente
→ Confirma con voz
```

### 2. Automatización Compleja
```
Claude recibe instrucción: "Fill out this form"
→ Lee formulario con OCR
→ Identifica campos
→ Completa datos uno por uno
→ Submit final
→ Verifica éxito
```

### 3. Accesibilidad Mejorada
```
Usuario con discapacidad: "Help me navigate this website"
→ Claude lee estructura de página
→ Guía al usuario paso a paso
→ Ejecuta clicks por el usuario
→ Confirma cada acción con voz
```

---

## 🚀 Roadmap Futuro

### Mejoras Planificadas
- [ ] WebSocket para comunicación en tiempo real
- [ ] Autenticación con API keys
- [ ] Rate limiting para seguridad
- [ ] Soporte para múltiples monitores
- [ ] Recording de macros
- [ ] Export/import de workflows
- [ ] Dashboard web para monitoreo

---

## 📚 Referencias

### Archivos Clave
- `Services/AionRemoteControlService.cs` - Servidor HTTP
- `Services/ClaudeCodeIntegrationService.cs` - Integración Claude
- `claude-aion-cli.js` - Cliente CLI
- `App.xaml.cs` - Inicialización de servicios
- `MainWindow.xaml.cs` - UI principal

### Endpoints Documentados
- Ver sección **API Endpoints** arriba
- Ver `claude-aion-cli.js help` para lista completa

---

## ✅ Checklist de Verificación

- [x] ✅ Servidor HTTP implementado
- [x] ✅ 14 API endpoints funcionando
- [x] ✅ Cliente CLI creado
- [x] ✅ Integración bidireccional completa
- [x] ✅ Logging exhaustivo
- [x] ✅ Manejo de errores robusto
- [x] ✅ CORS configurado
- [x] ✅ Documentación completa
- [ ] ⏳ Tests automatizados
- [ ] ⏳ WebSocket support

---

## 🎉 Resultado Final

**AION Desktop Assistant ahora soporta control bidireccional completo:**

1. 🎤 **Usuarios → AION → Claude**: Asistencia AI por voz
2. 🤖 **Claude → HTTP API → AION**: Control remoto programático
3. 🔄 **Loop completo**: Claude puede ver, decidir, actuar y confirmar

**¡El sistema de accesibilidad es ahora totalmente autónomo y controlable por AI!**

---

<div align="center">

### 🌟 **INTEGRACIÓN BIDIRECCIONAL COMPLETA**

**AION ↔️ Claude Code = Accesibilidad AI del Futuro**

![Bidirectional](https://img.shields.io/badge/Bidirectional-100%25-brightgreen?style=for-the-badge)
![API](https://img.shields.io/badge/REST%20API-14%20Endpoints-blue?style=for-the-badge)
![Claude](https://img.shields.io/badge/Claude%20Code-Integrated-purple?style=for-the-badge)

</div>
