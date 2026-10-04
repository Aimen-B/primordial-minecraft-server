"""Static download hub and same-host reverse proxy for launcher authentication."""
import http.client
import http.server
import json
import os

ROOT = os.environ.get("PRIMORDIAL_WEB_ROOT", "/server/web")
LATEST = "https://github.com/Aimen-B/primordial-minecraft-server/releases/latest/download/"

class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=ROOT, **kwargs)

    def do_GET(self):
        req_path = self.path.split("?", 1)[0]
        if req_path in ("/PrimordialLauncher.exe", "/Primordial-Adventures-Portable.zip"):
            filename = req_path.lstrip("/")
            self.send_response(302)
            self.send_header("Location", LATEST + filename)
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            return
        if self.path.startswith("/api/"):
            self.send_error(405)
            return
        super().do_GET()

    def do_POST(self):
        if self.path == "/api/whitelist":
            self.reply(403, b'{"success":false,"message":"Ask the host to approve your exact nickname."}')
            return
        if self.path in ("/api/auth/status", "/api/auth/session"):
            self.handle_auth_proxy()
            return
        self.send_error(404)

    def handle_auth_proxy(self):
        # Only the HTTPS reverse proxy serves authentication.
        if self.headers.get("X-Forwarded-Proto", "").split(",")[0].strip() != "https":
            self.reply(426, b'{"status":"https_required"}')
            return
        try:
            size = int(self.headers.get("Content-Length", "0"))
            if not 0 < size <= 4096:
                self.reply(413, b'{"status":"too_large"}')
                return
            body = self.rfile.read(size)
            connection = http.client.HTTPConnection("127.0.0.1", 8081, timeout=15)
            try:
                connection.request("POST", self.path, body, {"Content-Type": "application/json"})
                response = connection.getresponse()
                self.reply(response.status, response.read(8193))
            finally:
                connection.close()
        except (ValueError, OSError, http.client.HTTPException):
            self.reply(503, b'{"status":"unavailable"}')

    def reply(self, status, body):
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Cache-Control", "no-store")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, format, *args):
        # No passwords, request bodies, tickets, or player names are logged.
        print("[Web] " + self.command + " " + self.path.split("?", 1)[0], flush=True)

if __name__ == "__main__":
    http.server.ThreadingHTTPServer(("0.0.0.0", 8080), Handler).serve_forever()
