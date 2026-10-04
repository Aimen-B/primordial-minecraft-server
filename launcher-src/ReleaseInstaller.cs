using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace PrimordialLauncher {
    public sealed class ReleaseAsset { public string url; public string sha256; public long size; }
    public sealed class ReleaseManifest { public int schema; public string version; public Dictionary<string, ReleaseAsset> assets; }
    public sealed class PackageFile { public string path; public string sha256; }
    public sealed class PackageManifest { public string version; public PackageFile[] files; }
    public static class ReleaseInstaller {
        public const string ManifestUrl = "https://github.com/Aimen-B/primordial-minecraft-server/releases/latest/download/release.json";
        public static ReleaseManifest GetRelease() {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var client = new WebClient()) {
                var result = new JavaScriptSerializer().Deserialize<ReleaseManifest>(client.DownloadString(ManifestUrl));
                if (result.schema != 1 || result.assets == null) throw new InvalidDataException("Unsupported release manifest.");
                foreach (var asset in result.assets.Values) {
                    var url = new Uri(asset.url);
                    if (url.Scheme != "https" || url.Host != "github.com" || !url.AbsolutePath.StartsWith("/Aimen-B/primordial-minecraft-server/releases/download/"))
                        throw new InvalidDataException("Untrusted download URL.");
                    if (asset.size <= 0 || asset.sha256 == null || asset.sha256.Length != 64) throw new InvalidDataException("Invalid checksum information.");
                }
                return result;
            }
        }
        public static string Hash(string path) {
            using (var stream = File.OpenRead(path)) using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        private static string SafePath(string root, string relative) {
            if (String.IsNullOrWhiteSpace(relative) || relative.Contains(":") || Path.IsPathRooted(relative)) throw new InvalidDataException("Invalid package path.");
            string path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Package path escapes its directory.");
            return path;
        }
        public static void Install(string zip, string target, ReleaseAsset asset, string expectedVersion) {
            if (new FileInfo(zip).Length != asset.size || Hash(zip) != asset.sha256) throw new InvalidDataException("Download checksum failed. Download again.");
            string stage = Path.Combine(Path.GetTempPath(), "Primordial-stage-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);
            try {
                using (var archive = ZipFile.OpenRead(zip)) {
                    foreach (var entry in archive.Entries) {
                        string output = SafePath(stage, entry.FullName);
                        if (String.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(output); continue; }
                        Directory.CreateDirectory(Path.GetDirectoryName(output));
                        entry.ExtractToFile(output);
                    }
                }
                string manifestFile = Path.Combine(stage, "package-files.json");
                PackageManifest manifest = null;
                if (File.Exists(manifestFile)) {
                    try {
                        manifest = new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 }.Deserialize<PackageManifest>(File.ReadAllText(manifestFile));
                    } catch { manifest = null; }
                }

                string backup = Path.Combine(target,"repair-backups",DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N"));
                var replaced = new List<string>();
                var created = new List<string>();
                var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Directory.CreateDirectory(target);
                try {
                    if (manifest != null && manifest.files != null) {
                        foreach (var file in manifest.files) {
                            expected.Add(file.path.Replace('\\','/'));
                            string relative = file.path;
                            string output = SafePath(target,relative);
                            if (Preserve(relative) && File.Exists(output)) continue;
                            if (Path.GetFullPath(output).Equals(System.Windows.Forms.Application.ExecutablePath,StringComparison.OrdinalIgnoreCase)) continue;
                            Directory.CreateDirectory(Path.GetDirectoryName(output));
                            if (File.Exists(output)) {
                                string previous = SafePath(backup,relative);
                                Directory.CreateDirectory(Path.GetDirectoryName(previous));
                                File.Copy(output,previous);
                                replaced.Add(relative);
                            } else created.Add(relative);
                            File.Copy(SafePath(stage,relative),output,true);
                        }
                        File.Copy(manifestFile,Path.Combine(target,"package-files.json"),true);
                    } else {
                        foreach (string file in Directory.GetFiles(stage,"*",SearchOption.AllDirectories)) {
                            string relative = file.Substring(stage.Length + 1).Replace('\\','/');
                            expected.Add(relative);
                            string output = SafePath(target,relative);
                            if (Preserve(relative) && File.Exists(output)) continue;
                            if (Path.GetFullPath(output).Equals(System.Windows.Forms.Application.ExecutablePath,StringComparison.OrdinalIgnoreCase)) continue;
                            Directory.CreateDirectory(Path.GetDirectoryName(output));
                            if (File.Exists(output)) {
                                string previous = SafePath(backup,relative);
                                Directory.CreateDirectory(Path.GetDirectoryName(previous));
                                File.Copy(output,previous);
                                replaced.Add(relative);
                            } else created.Add(relative);
                            File.Copy(SafePath(stage,relative),output,true);
                        }
                    }
                    string mods = Path.Combine(target,"launcher","instances","Primordial-Adventures",".minecraft","mods");
                    if (Directory.Exists(mods)) foreach (string mod in Directory.GetFiles(mods)) {
                        string relative = mod.Substring(target.Length + 1).Replace('\\','/');
                        if (!expected.Contains(relative)) {
                            string previous = SafePath(backup,relative);
                            Directory.CreateDirectory(Path.GetDirectoryName(previous));
                            File.Move(mod,previous);
                        }
                    }
                } catch {
                    foreach (string relative in created) { string path=SafePath(target,relative); if(File.Exists(path))File.Delete(path); }
                    foreach (string relative in replaced) File.Copy(SafePath(backup,relative),SafePath(target,relative),true);
                    throw;
                }
            } finally { Directory.Delete(stage,true); }
        }
        private static bool Preserve(string path) {
            string p=path.Replace('\\','/').ToLowerInvariant();
            return p.EndsWith("/options.txt") || p.EndsWith("/servers.dat") || p.EndsWith("/instance.cfg") || p=="launcher/elyprismlauncher.cfg";
        }
    }
}
