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
            self.handle_whitelist()
            return
        if self.path in ("/api/auth/status", "/api/auth/session"):
            self.handle_auth_proxy()
            return
        self.send_error(404)

    def handle_whitelist(self):
        try:
            size = int(self.headers.get("Content-Length", "0"))
            if not 0 < size <= 4096:
                self.reply(413, b'{"success":false,"message":"Payload too large"}')
                return
            payload = json.loads(self.rfile.read(size).decode("utf-8"))
            nickname = payload.get("nickname", "").strip()
            invite_code = payload.get("invite_code", "").strip()

            import re, hashlib, uuid
            if not re.match(r"^[a-zA-Z0-9_]{3,16}$", nickname):
                self.reply(400, json.dumps({
                    "success": False,
                    "message": "Nickname must be 3-16 characters (letters, numbers, underscores)."
                }).encode())
                return

            expected_code = os.environ.get("INVITE_CODE", "adventure").strip()
            if invite_code.lower() != expected_code.lower():
                self.reply(403, json.dumps({
                    "success": False,
                    "message": "Invalid invite passcode. Ask Primordial for the secret invite code!"
                }).encode())
                return

            # Compute offline UUID matching Minecraft Java standard
            md5 = bytearray(hashlib.md5(f"OfflinePlayer:{nickname}".encode("utf-8")).digest())
            md5[6] = (md5[6] & 0x0f) | 0x30
            md5[8] = (md5[8] & 0x3f) | 0x80
            player_uuid = str(uuid.UUID(bytes=bytes(md5)))

            whitelist_path = os.environ.get("WHITELIST_PATH", "/server/whitelist.json")
            whitelist = []
            if os.path.exists(whitelist_path):
                try:
                    with open(whitelist_path, "r", encoding="utf-8") as f:
                        whitelist = json.load(f)
                except Exception:
                    whitelist = []

            exists = any(entry.get("name", "").lower() == nickname.lower() for entry in whitelist)
            if not exists:
                whitelist.append({"uuid": player_uuid, "name": nickname})
                temp_path = whitelist_path + ".tmp"
                with open(temp_path, "w", encoding="utf-8") as f:
                    json.dump(whitelist, f, indent=2)
                os.replace(temp_path, whitelist_path)

            self.reply(200, json.dumps({
                "success": True,
                "message": f"Successfully whitelisted '{nickname}'! You can now launch and connect to mc.primordial.my!"
            }).encode())
        except Exception as e:
            self.reply(500, json.dumps({"success": False, "message": f"Internal server error: {str(e)}"}).encode())

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
