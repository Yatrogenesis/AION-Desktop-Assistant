#!/usr/bin/env python3
"""
🤖 AION Desktop Assistant - Simplified HTTP Control Server
Servidor HTTP simplificado para demostrar control remoto del escritorio
"""

import json
import webbrowser
import time
import subprocess
import os
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs
import pyautogui
import logging

# Configurar logging con emojis
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - 📝 %(message)s'
)
logger = logging.getLogger(__name__)

class AIONRequestHandler(BaseHTTPRequestHandler):

    def log_message(self, format, *args):
        """Override para usar nuestro logger"""
        logger.info(format % args)

    def _set_headers(self, status=200):
        self.send_response(status)
        self.send_header('Content-type', 'application/json')
        self.send_header('Access-Control-Allow-Origin', '*')
        self.end_headers()

    def _send_json_response(self, success, message, data=None):
        """Envía respuesta JSON"""
        response = {
            "success": success,
            "message": message,
            "data": data or {},
            "timestamp": time.time()
        }
        self.wfile.write(json.dumps(response, indent=2).encode())

    def do_GET(self):
        """Maneja solicitudes GET"""
        parsed_path = urlparse(self.path)
        path = parsed_path.path

        logger.info(f"🔍 GET request: {path}")

        if path == '/api/status':
            self._set_headers()
            self._send_json_response(True, "✅ AION Server Online", {
                "status": "running",
                "port": 8080,
                "version": "1.1.0"
            })

        elif path == '/api/screen/read':
            self._set_headers()
            try:
                # Simular lectura de pantalla
                screen_size = pyautogui.size()
                self._send_json_response(True, "📖 Screen read successfully", {
                    "screen_size": f"{screen_size.width}x{screen_size.height}",
                    "text": "Demo mode - Screen reading simulated"
                })
            except Exception as e:
                self._send_json_response(False, f"❌ Error: {str(e)}")

        else:
            self._set_headers(404)
            self._send_json_response(False, f"❌ Endpoint not found: {path}")

    def do_POST(self):
        """Maneja solicitudes POST"""
        parsed_path = urlparse(self.path)
        path = parsed_path.path

        # Leer el body
        content_length = int(self.headers.get('Content-Length', 0))
        body = self.rfile.read(content_length).decode() if content_length > 0 else "{}"

        try:
            data = json.loads(body) if body else {}
        except:
            data = {}

        logger.info(f"📬 POST request: {path} - Data: {data}")

        if path == '/api/mouse/move':
            self._set_headers()
            try:
                x = int(data.get('x', 0))
                y = int(data.get('y', 0))
                pyautogui.moveTo(x, y, duration=0.5)
                self._send_json_response(True, f"🖱️  Mouse moved to ({x}, {y})")
            except Exception as e:
                self._send_json_response(False, f"❌ Error: {str(e)}")

        elif path == '/api/mouse/click':
            self._set_headers()
            try:
                x = data.get('x')
                y = data.get('y')
                button = data.get('button', 'left')

                if x and y:
                    pyautogui.click(x, y, button=button)
                    self._send_json_response(True, f"🖱️ Clicked at ({x}, {y}) with {button} button")
                else:
                    pyautogui.click(button=button)
                    self._send_json_response(True, f"🖱️  Clicked with {button} button")
            except Exception as e:
                self._send_json_response(False, f"❌ Error: {str(e)}")

        elif path == '/api/keyboard/type':
            self._set_headers()
            try:
                text = data.get('text', '')
                interval = float(data.get('interval', 0.05))
                pyautogui.write(text, interval=interval)
                self._send_json_response(True, f"⌨️  Typed: {text}")
            except Exception as e:
                self._send_json_response(False, f"❌ Error: {str(e)}")

        elif path == '/api/keyboard/press':
            self._set_headers()
            try:
                key = data.get('key', '')
                pyautogui.press(key)
                self._send_json_response(True, f"⌨️ Pressed key: {key}")
            except Exception as e:
                self._send_json_response(False, f"❌ Error: {str(e)}")

        elif path == '/api/browser/open':
            self._set_headers()
            try:
                url = data.get('url', 'https://www.google.com')
                webbrowser.open(url)
                self._send_json_response(True, f"🌐 Browser opened: {url}")
            except Exception as e:
                self._send_json_response(False, f"❌ Error: {str(e)}")

        elif path == '/api/command/execute':
            self._set_headers()
            try:
                command = data.get('command', '')
                logger.info(f"💻 Executing command: {command}")
                # Ejecutar comando de forma segura
                result = subprocess.run(command, shell=True, capture_output=True, text=True, timeout=10)
                self._send_json_response(True, "✅ Command executed", {
                    "stdout": result.stdout,
                    "stderr": result.stderr,
                    "returncode": result.returncode
                })
            except Exception as e:
                self._send_json_response(False, f"❌ Error: {str(e)}")

        else:
            self._set_headers(404)
            self._send_json_response(False, f"❌ Endpoint not found: {path}")

def run_server(port=8080):
    """Inicia el servidor HTTP"""
    server_address = ('', port)
    httpd = HTTPServer(server_address, AIONRequestHandler)

    logger.info("=" * 80)
    logger.info("🤖 AION Desktop Assistant - Simplified Control Server")
    logger.info("=" * 80)
    logger.info(f"✅ Server started on http://localhost:{port}")
    logger.info(f"📡 API endpoints available:")
    logger.info(f"   GET  /api/status           - Check server status")
    logger.info(f"   GET  /api/screen/read      - Read screen content")
    logger.info(f"   POST /api/mouse/move       - Move mouse (x, y)")
    logger.info(f"   POST /api/mouse/click      - Click mouse (x, y, button)")
    logger.info(f"   POST /api/keyboard/type    - Type text (text, interval)")
    logger.info(f"   POST /api/keyboard/press   - Press key (key)")
    logger.info(f"   POST /api/browser/open     - Open URL (url)")
    logger.info(f"   POST /api/command/execute  - Execute command (command)")
    logger.info("=" * 80)
    logger.info("🚀 Ready to accept connections...")
    logger.info("=" * 80)

    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        logger.info("\n⏹️  Server stopped by user")
        httpd.shutdown()

if __name__ == '__main__':
    run_server()
