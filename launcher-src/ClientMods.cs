using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace PrimordialLauncher {
    public sealed class ClientMod { public string filename; public string sha256; public string url; }
    public sealed class ClientModManifest { public int schema; public string minecraft; public string neoforge; public ClientMod[] mods; }
    public sealed class GameComponent { public string uid; public string version; }
    public sealed class GameComponents { public GameComponent[] components; }

    public static class ClientMods {
        public static ClientModManifest Parse(string json) {
            var manifest = new JavaScriptSerializer().Deserialize<ClientModManifest>(json);
            if (manifest == null || manifest.schema != 1 || manifest.minecraft != "1.21.1" || manifest.neoforge != "21.1.252" || manifest.mods == null || manifest.mods.Length == 0)
                throw new InvalidDataException("Unsupported client mod manifest. Update the launcher and game together.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in manifest.mods) {
                if (mod == null || !Regex.IsMatch(mod.filename ?? "", @"^[A-Za-z0-9_.+\-]+\.jar$") || !names.Add(mod.filename) || !Regex.IsMatch(mod.sha256 ?? "", "^[a-f0-9]{64}$"))
                    throw new InvalidDataException("Invalid or duplicate mod entry.");
                if (mod.url != null) {
                    Uri url;
                    if (!Uri.TryCreate(mod.url, UriKind.Absolute, out url) || url.Scheme != "https" ||
                        !(url.Host == "cdn.modrinth.com" || url.Host == "edge.forgecdn.net" || url.Host == "mediafilez.forgecdn.net" ||
                          (url.Host == "raw.githubusercontent.com" && url.AbsolutePath.StartsWith("/Aimen-B/primordial-minecraft-server/")) ||
                          (url.Host == "github.com" && url.AbsolutePath.StartsWith("/Aimen-B/primordial-minecraft-server/releases/download/"))))
                        throw new InvalidDataException("Untrusted mod download URL.");
                }
            }
            return manifest;
        }

        public static ClientModManifest Embedded() {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PrimordialLauncher.client-mods.json")) {
                if (stream == null) throw new InvalidDataException("The launcher was built without its mod manifest.");
                using (var reader = new StreamReader(stream)) return Parse(reader.ReadToEnd());
            }
        }

        // Old portable packages can contain the authentication bridge in addition to the base pack.
        // Keep it only when it is explicitly covered by the installed package's checksum manifest.
        public static ClientModManifest LegacyBundle(ClientModManifest manifest, string installRoot) {
            string path = Path.Combine(installRoot, "package-files.json");
            if (!File.Exists(path)) return manifest;
            var package = new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 }.Deserialize<PackageManifest>(File.ReadAllText(path));
            var mods = new List<ClientMod>(manifest.mods);
            const string prefix = "launcher/instances/Primordial-Adventures/.minecraft/mods/";
            foreach (var file in package.files ?? new PackageFile[0]) {
                string relative = file.path.Replace('\\', '/');
                if (!relative.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                string name = relative.Substring(prefix.Length);
                if (!name.StartsWith("primordial-", StringComparison.OrdinalIgnoreCase)) continue;
                mods.Add(new ClientMod { filename = name, sha256 = file.sha256 });
            }
            manifest.mods = mods.ToArray();
            return Parse(new JavaScriptSerializer().Serialize(manifest));
        }

        public static void Download(string url, string path) {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var client = new TimedWebClient()) client.DownloadFile(url, path);
        }

        public static ClientModManifest ForLaunch(string installRoot, string instance, Action<string> log) {
            string cache = Path.Combine(instance, ".primordial-client-mods.json");
            ReleaseManifest release;
            try { release = ReleaseInstaller.GetRelease(); }
            catch (WebException) {
                log("Release check unavailable; verifying the last known pack before launch.");
                return File.Exists(cache) ? Parse(File.ReadAllText(cache)) : LegacyBundle(Embedded(), installRoot);
            }
            ReleaseAsset asset;
            if (!release.assets.TryGetValue("client-mods.json", out asset))
                return LegacyBundle(Embedded(), installRoot);
            string temporary = Path.Combine(Path.GetTempPath(), "Primordial-mod-manifest-" + Guid.NewGuid().ToString("N"));
            try {
                Download(asset.url, temporary);
                if (new FileInfo(temporary).Length != asset.size || ReleaseInstaller.Hash(temporary) != asset.sha256)
                    throw new InvalidDataException("Client mod manifest checksum failed.");
                return Parse(File.ReadAllText(temporary));
            } finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public static void Cache(string instance, ClientModManifest manifest) {
            string path = Path.Combine(instance, ".primordial-client-mods.json");
            string temporary = path + ".partial";
            File.WriteAllText(temporary, new JavaScriptSerializer().Serialize(manifest));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }

        public static void Verify(string folder, ClientModManifest manifest) {
            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in manifest.mods) {
                expected.Add(mod.filename);
                string path = Path.Combine(folder, mod.filename);
                if (!File.Exists(path) || ReleaseInstaller.Hash(path) != mod.sha256)
                    throw new InvalidDataException("Required mod is missing or changed: " + mod.filename);
            }
            foreach (string path in Directory.GetFiles(folder, "*.jar"))
                if (!expected.Contains(Path.GetFileName(path))) throw new InvalidDataException("Conflicting mod: " + Path.GetFileName(path));
        }

        public static void VerifyGameVersion(string instance, ClientModManifest manifest) {
            var game = new JavaScriptSerializer().Deserialize<GameComponents>(File.ReadAllText(Path.Combine(instance, "mmc-pack.json")));
            bool minecraft = false, neoforge = false;
            foreach (var component in game.components ?? new GameComponent[0]) {
                if (component.uid == "net.minecraft" && component.version == manifest.minecraft) minecraft = true;
                if (component.uid == "net.neoforged" && component.version == manifest.neoforge) neoforge = true;
            }
            if (!minecraft || !neoforge) throw new InvalidDataException("Minecraft or NeoForge version does not match the pack. Install the current portable game bundle.");
        }

        public static void Repair(string folder, ClientModManifest manifest, Action<string> log, Action<string, string> download) {
            // Validate the entire manifest before writing anything.
            manifest = Parse(new JavaScriptSerializer().Serialize(manifest));
            Directory.CreateDirectory(folder);
            string lockPath = Path.Combine(folder, ".primordial-mods.lock");
            using (var guard = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
                string parent = Directory.GetParent(Path.GetFullPath(folder)).FullName;
                string stage = Path.Combine(parent, "primordial-mod-stage-" + Guid.NewGuid().ToString("N"));
                string backup = Path.Combine(parent, "primordial-mod-backups", DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N"));
                var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var replacements = new List<string>();
                var moved = new List<string>();
                var installed = new List<string>();
                Directory.CreateDirectory(stage);
                try {
                    // Stage and verify every download before changing the active mod folder.
                    foreach (var mod in manifest.mods) {
                        wanted.Add(mod.filename);
                        string current = Path.Combine(folder, mod.filename);
                        if (File.Exists(current) && ReleaseInstaller.Hash(current) == mod.sha256) continue;
                        if (String.IsNullOrEmpty(mod.url)) throw new InvalidDataException("Reinstall the game to restore " + mod.filename);
                        log("Repairing " + mod.filename);
                        string staged = Path.Combine(stage, mod.filename);
                        download(mod.url, staged);
                        if (ReleaseInstaller.Hash(staged) != mod.sha256) throw new InvalidDataException("Mod download checksum failed: " + mod.filename);
                        using (var jar = ZipFile.OpenRead(staged)) { if (jar.Entries.Count == 0) throw new InvalidDataException("Empty mod jar: " + mod.filename); }
                        replacements.Add(mod.filename);
                    }
                    try {
                        foreach (string current in Directory.GetFiles(folder, "*.jar")) {
                            string name = Path.GetFileName(current);
                            if (wanted.Contains(name) && !replacements.Contains(name)) continue;
                            Directory.CreateDirectory(backup);
                            File.Move(current, Path.Combine(backup, name));
                            moved.Add(name);
                        }
                        foreach (string name in replacements) {
                            File.Move(Path.Combine(stage, name), Path.Combine(folder, name));
                            installed.Add(name);
                        }
                        Verify(folder, manifest);
                    } catch {
                        foreach (string name in installed) File.Delete(Path.Combine(folder, name));
                        foreach (string name in moved) File.Move(Path.Combine(backup, name), Path.Combine(folder, name));
                        throw;
                    }
                    if (moved.Count > 0) log("Previous or conflicting mods backed up to " + backup);
                    log("All " + manifest.mods.Length + " client mods verified.");
                } finally { Directory.Delete(stage, true); }
            }
        }
    }

    public sealed class TimedWebClient : WebClient {
        protected override WebRequest GetWebRequest(Uri address) {
            var request = base.GetWebRequest(address);
            request.Timeout = 15000;
            var http = request as HttpWebRequest;
            if (http != null) http.ReadWriteTimeout = 60000;
            return request;
        }
    }
}
