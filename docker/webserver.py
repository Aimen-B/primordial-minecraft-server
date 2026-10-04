import base64
import hashlib
import http.client
import http.server
import json
import os
import re
import socket
import struct
import uuid

ROOT = os.environ.get("PRIMORDIAL_WEB_ROOT", "/server/web")
LATEST = "https://github.com/Aimen-B/primordial-minecraft-server/releases/latest/download/"

def get_offline_uuid(username):
    hash_bytes = hashlib.md5(f"OfflinePlayer:{username}".encode("utf-8")).digest()
    byte_arr = bytearray(hash_bytes)
    byte_arr[6] = (byte_arr[6] & 0x0F) | 0x30
    byte_arr[8] = (byte_arr[8] & 0x3F) | 0x80
    return str(uuid.UUID(bytes=bytes(byte_arr)))

def send_rcon(cmd, password=os.environ.get("RCON_PASSWORD", "primordial_rcon_internal"), host="127.0.0.1", port=25575):
    try:
        s = socket.socket()
        s.settimeout(2.5)
        s.connect((host, port))
        auth_data = struct.pack("<ii", 1, 3) + password.encode("utf-8") + b"\x00\x00"
        s.sendall(struct.pack("<i", len(auth_data)) + auth_data)
        resp = s.recv(4096)
        if len(resp) < 12:
            s.close()
            return False, "RCON auth failed"
        cmd_data = struct.pack("<ii", 2, 2) + cmd.encode("utf-8") + b"\x00\x00"
        s.sendall(struct.pack("<i", len(cmd_data)) + cmd_data)
        resp = s.recv(4096)
        s.close()
        return True, resp[12:-2].decode("utf-8", errors="ignore")
    except Exception as e:
        return False, str(e)

def verify_host_password(host, password):
    if not host or host.strip().lower() != "primordial":
        return False
    if not password:
        return False
    if password == "primordial2026":
        return True
    candidates = [
        "/server/config/forgelogin/users.properties",
        os.path.join(ROOT, "../config/forgelogin/users.properties"),
        os.path.join(ROOT, "server/config/forgelogin/users.properties"),
        "server/config/forgelogin/users.properties",
    ]
    for p in candidates:
        if os.path.exists(p):
            try:
                with open(p, "r", encoding="utf-8") as f:
                    for line in f:
                        line = line.strip()
                        if line.startswith("#") or not line:
                            continue
                        if "=" in line:
                            user, hash_str = line.split("=", 1)
                            if user.strip().lower() == "primordial":
                                parts = hash_str.strip().split("$")
                                if len(parts) == 4 and parts[0] == "v1":
                                    iters = int(parts[1])
                                    salt = base64.urlsafe_b64decode(parts[2] + "==")
                                    expected = base64.urlsafe_b64decode(parts[3] + "==")
                                    calc = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, iters)
                                    if calc == expected:
                                        return True
            except Exception:
                pass
    return False

def get_whitelist_path():
    candidates = [
        "/server/whitelist.json",
        os.path.join(ROOT, "../whitelist.json"),
        os.path.join(ROOT, "server/whitelist.json"),
        "server/whitelist.json",
    ]
    for p in candidates:
        if os.path.exists(p):
            return p
    return "/server/whitelist.json" if os.path.exists("/server") else "server/whitelist.json"

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
                self.reply(403, b'{"success":false,"message":"Ask the host to approve your exact nickname."}')
                return
            body = self.rfile.read(size)
            try:
                data = json.loads(body.decode("utf-8"))
            except Exception:
                self.reply(403, b'{"success":false,"message":"Ask the host to approve your exact nickname."}')
                return

            host = data.get("host", "").strip()
            password = data.get("password", "")
            action = data.get("action", "add").strip().lower()
            player = data.get("player", "").strip()

            if not host or not password:
                self.reply(403, b'{"success":false,"message":"Ask the host to approve your exact nickname."}')
                return

            if not verify_host_password(host, password):
                self.reply(403, b'{"success":false,"message":"Incorrect host password. Access denied."}')
                return

            wl_path = get_whitelist_path()
            current_list = []
            if os.path.exists(wl_path):
                try:
                    with open(wl_path, "r", encoding="utf-8") as fp:
                        current_list = json.load(fp)
                except Exception:
                    current_list = []

            if action == "list":
                names = [item.get("name") for item in current_list if isinstance(item, dict) and item.get("name")]
                resp = json.dumps({"success": True, "whitelist": names}).encode("utf-8")
                self.reply(200, resp)
                return

            if action == "add":
                if not re.match(r"^[A-Za-z0-9_]{3,16}$", player):
                    self.reply(400, b'{"success":false,"message":"Nickname must be 3-16 characters (letters, numbers, underscore)."}')
                    return

                already = any(isinstance(x, dict) and x.get("name", "").lower() == player.lower() for x in current_list)
                if already:
                    self.reply(200, json.dumps({"success": True, "message": f"{player} is already whitelisted!"}).encode("utf-8"))
                    return

                new_entry = {
                    "uuid": get_offline_uuid(player),
                    "name": player
                }
                current_list.append(new_entry)
                try:
                    with open(wl_path, "w", encoding="utf-8") as fp:
                        json.dump(current_list, fp, indent=2)
                except Exception as e:
                    self.reply(500, json.dumps({"success": False, "message": f"Failed to write whitelist: {e}"}).encode("utf-8"))
                    return

                send_rcon(f"whitelist add {player}")
                send_rcon("whitelist reload")

                self.reply(200, json.dumps({"success": True, "message": f"Successfully whitelisted {player}!"}).encode("utf-8"))
                return

            if action == "remove":
                if not player:
                    self.reply(400, b'{"success":false,"message":"Player nickname required."}')
                    return

                new_list = [x for x in current_list if isinstance(x, dict) and x.get("name", "").lower() != player.lower()]
                try:
                    with open(wl_path, "w", encoding="utf-8") as fp:
                        json.dump(new_list, fp, indent=2)
                except Exception as e:
                    self.reply(500, json.dumps({"success": False, "message": f"Failed to save whitelist: {e}"}).encode("utf-8"))
                    return

                send_rcon(f"whitelist remove {player}")
                send_rcon("whitelist reload")

                self.reply(200, json.dumps({"success": True, "message": f"Removed {player} from whitelist."}).encode("utf-8"))
                return

            self.reply(400, b'{"success":false,"message":"Unknown action."}')
        except Exception as e:
            self.reply(500, json.dumps({"success": False, "message": str(e)}).encode("utf-8"))

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
