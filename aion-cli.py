#!/usr/bin/env python3
"""
AION Desktop Assistant - Command Line Interface
CLI para control remoto de AION Desktop Assistant
"""

import sys
import json
import requests
from typing import Optional, Dict, Any

AION_API_URL = "http://localhost:8080"

class AIONCli:
    def __init__(self, base_url: str = AION_API_URL):
        self.base_url = base_url

    def _request(self, method: str, endpoint: str, data: Optional[Dict] = None) -> Dict[str, Any]:
        """Realiza una petición HTTP a la API de AION"""
        url = f"{self.base_url}{endpoint}"
        try:
            if method == "GET":
                response = requests.get(url, timeout=5)
            elif method == "POST":
                response = requests.post(url, json=data, timeout=5)
            else:
                return {"success": False, "message": f"Método no soportado: {method}"}

            return response.json()
        except requests.exceptions.ConnectionError:
            return {"success": False, "message": "No se puede conectar al servidor AION. Asegúrate de que esté ejecutándose."}
        except Exception as e:
            return {"success": False, "message": f"Error: {str(e)}"}

    def status(self) -> Dict[str, Any]:
        """Obtiene el estado del servidor"""
        return self._request("GET", "/api/status")

    def read_screen(self) -> Dict[str, Any]:
        """Lee el contenido de la pantalla"""
        return self._request("GET", "/api/screen/read")

    def move_mouse(self, x: int, y: int) -> Dict[str, Any]:
        """Mueve el mouse a las coordenadas especificadas"""
        return self._request("POST", "/api/mouse/move", {"x": x, "y": y})

    def click(self, x: Optional[int] = None, y: Optional[int] = None, button: str = "left") -> Dict[str, Any]:
        """Hace clic en la posición actual o en las coordenadas especificadas"""
        data = {"button": button}
        if x is not None and y is not None:
            data.update({"x": x, "y": y})
        return self._request("POST", "/api/mouse/click", data)

    def type_text(self, text: str, interval: float = 0.05) -> Dict[str, Any]:
        """Escribe texto"""
        return self._request("POST", "/api/keyboard/type", {"text": text, "interval": interval})

    def press_key(self, key: str) -> Dict[str, Any]:
        """Presiona una tecla"""
        return self._request("POST", "/api/keyboard/press", {"key": key})

    def open_browser(self, url: str) -> Dict[str, Any]:
        """Abre una URL en el navegador"""
        return self._request("POST", "/api/browser/open", {"url": url})

    def execute_command(self, command: str) -> Dict[str, Any]:
        """Ejecuta un comando del sistema"""
        return self._request("POST", "/api/command/execute", {"command": command})


def print_result(result: Dict[str, Any]):
    """Imprime el resultado de una operación"""
    import sys
    import codecs

    # Configurar encoding UTF-8 para Windows
    if sys.platform == 'win32':
        sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'strict')

    if result.get("success"):
        print(f"OK: {result.get('message')}")
        if result.get("data"):
            print(f"Data: {json.dumps(result['data'], indent=2, ensure_ascii=False)}")
    else:
        print(f"ERROR: {result.get('message')}")
    sys.exit(0 if result.get("success") else 1)


def show_help():
    """Muestra la ayuda"""
    help_text = """
AION Desktop Assistant - CLI

Uso: python aion-cli.py <comando> [argumentos]

Comandos disponibles:

  status                              - Verifica el estado del servidor
  read-screen                         - Lee el contenido de la pantalla
  move-mouse <x> <y>                  - Mueve el mouse a (x, y)
  click [x] [y] [button]              - Hace clic (left/right/middle)
  type <texto>                        - Escribe texto
  press <tecla>                       - Presiona una tecla (enter, tab, etc)
  open <url>                          - Abre URL en navegador
  exec <comando>                      - Ejecuta comando del sistema

Ejemplos:

  python aion-cli.py status
  python aion-cli.py move-mouse 500 300
  python aion-cli.py click 500 300 left
  python aion-cli.py type "Hola mundo"
  python aion-cli.py press enter
  python aion-cli.py open "https://www.google.com"
  python aion-cli.py exec "dir"
"""
    print(help_text)
    sys.exit(0)


def main():
    if len(sys.argv) < 2:
        show_help()

    cli = AIONCli()
    command = sys.argv[1].lower()

    if command in ["help", "-h", "--help"]:
        show_help()

    elif command == "status":
        result = cli.status()
        print_result(result)

    elif command == "read-screen":
        result = cli.read_screen()
        print_result(result)

    elif command == "move-mouse":
        if len(sys.argv) < 4:
            print("ERROR: Se requieren coordenadas x y")
            sys.exit(1)
        x, y = int(sys.argv[2]), int(sys.argv[3])
        result = cli.move_mouse(x, y)
        print_result(result)

    elif command == "click":
        if len(sys.argv) >= 4:
            x, y = int(sys.argv[2]), int(sys.argv[3])
            button = sys.argv[4] if len(sys.argv) > 4 else "left"
            result = cli.click(x, y, button)
        else:
            button = sys.argv[2] if len(sys.argv) > 2 else "left"
            result = cli.click(button=button)
        print_result(result)

    elif command == "type":
        if len(sys.argv) < 3:
            print("ERROR: Se requiere el texto a escribir")
            sys.exit(1)
        text = " ".join(sys.argv[2:])
        result = cli.type_text(text)
        print_result(result)

    elif command == "press":
        if len(sys.argv) < 3:
            print("ERROR: Se requiere la tecla a presionar")
            sys.exit(1)
        key = sys.argv[2]
        result = cli.press_key(key)
        print_result(result)

    elif command == "open":
        if len(sys.argv) < 3:
            print("ERROR: Se requiere la URL a abrir")
            sys.exit(1)
        url = sys.argv[2]
        result = cli.open_browser(url)
        print_result(result)

    elif command == "exec":
        if len(sys.argv) < 3:
            print("ERROR: Se requiere el comando a ejecutar")
            sys.exit(1)
        cmd = " ".join(sys.argv[2:])
        result = cli.execute_command(cmd)
        print_result(result)

    else:
        print(f"ERROR: Comando desconocido '{command}'")
        print("Usa 'python aion-cli.py help' para ver la ayuda")
        sys.exit(1)


if __name__ == "__main__":
    main()
