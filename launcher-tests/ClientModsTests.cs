using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web.Script.Serialization;
using PrimordialLauncher;

public static class ClientModsTests {
    private static int passed;
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Fails(Action action) { bool failed = false; try { action(); } catch { failed = true; } Assert(failed, "Expected operation to fail"); }
    private static string Jar(string root, string name, string data) {
        string path = Path.Combine(root, name);
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
            using (var stream = new StreamWriter(zip.CreateEntry("META-INF/neoforge.mods.toml").Open())) stream.Write(data);
        return path;
    }
    private static ClientModManifest Manifest(params string[] paths) {
        return new ClientModManifest { schema=1, minecraft="1.21.1", neoforge="21.1.252", mods=paths.Select(path => new ClientMod {
            filename=Path.GetFileName(path), sha256=ReleaseInstaller.Hash(path), url="https://cdn.modrinth.com/" + Path.GetFileName(path)
        }).ToArray() };
    }
    private static void Test(string name, Action<string> action) {
        string root = Path.Combine(Path.GetTempPath(), "Primordial-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { action(root); Console.WriteLine("PASS " + name); passed++; }
        finally { Directory.Delete(root, true); }
    }
    public static int Main() {
        try {
            var embedded = ClientMods.Embedded();
            Assert(embedded.mods.Any(mod => mod.filename.StartsWith("grapplemod-")), "Embedded manifest must include Grappling Hook");
            Assert(embedded.mods.Length == 31, "All base and extra client mods must be embedded");
            Test("missing Grappling Hook repaired, then offline verification uses no downloads", root => {
                string jar = Jar(root, "grapplemod-test.jar", "grapplemod");
                string folder = Path.Combine(root, "mods");
                var manifest = Manifest(jar); int downloads = 0;
                ClientMods.Repair(folder, manifest, _ => {}, (url, dest) => { downloads++; File.Copy(jar, dest); });
                Assert(downloads == 1, "Missing mod should download once"); ClientMods.Verify(folder, manifest);
                ClientMods.Repair(folder, manifest, _ => {}, (url, dest) => { throw new Exception("Offline"); });
                ClientMods.Verify(folder, manifest);
            });
            Test("corrupt and duplicate jars backed up; config untouched", root => {
                string jar = Jar(root, "grapplemod-test.jar", "grapplemod");
                string folder = Path.Combine(root, "mods"); Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "grapplemod-test.jar"), "corrupt");
                File.WriteAllText(Path.Combine(folder, "grapplemod-old.jar"), "old version");
                File.WriteAllText(Path.Combine(root, "options.txt"), "player settings");
                ClientMods.Repair(folder, Manifest(jar), _ => {}, (url, dest) => File.Copy(jar, dest));
                Assert(Directory.GetFiles(folder, "*.jar").Length == 1, "Duplicate remains");
                Assert(Directory.GetFiles(Path.Combine(root, "primordial-mod-backups"), "*.jar", SearchOption.AllDirectories).Length == 2, "Old jars must be backed up");
                Assert(File.ReadAllText(Path.Combine(root, "options.txt")) == "player settings", "Settings changed");
            });
            Test("bad checksum leaves active mods unchanged", root => {
                string jar = Jar(root, "grapplemod-test.jar", "grapplemod");
                string folder = Path.Combine(root, "mods"); Directory.CreateDirectory(folder);
                string current = Path.Combine(folder, "grapplemod-test.jar"); File.WriteAllText(current, "old");
                Fails(() => ClientMods.Repair(folder, Manifest(jar), _ => {}, (url, dest) => File.WriteAllText(dest, "broken")));
                Assert(File.ReadAllText(current) == "old", "Active jar changed before verification");
                Assert(!Directory.GetDirectories(root, "primordial-mod-stage-*").Any(), "Stage leaked");
            });
            Test("network failure after first staged jar leaves all active mods unchanged", root => {
                string first = Jar(root, "a.jar", "a"); string second = Jar(root, "b.jar", "b");
                string folder = Path.Combine(root, "mods"); Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "a.jar"), "old a"); File.WriteAllText(Path.Combine(folder, "b.jar"), "old b");
                int count = 0;
                Fails(() => ClientMods.Repair(folder, Manifest(first, second), _ => {}, (url, dest) => {
                    if (++count == 2) throw new IOException("Disconnected"); File.Copy(first, dest);
                }));
                Assert(File.ReadAllText(Path.Combine(folder, "a.jar")) == "old a" && File.ReadAllText(Path.Combine(folder, "b.jar")) == "old b", "Active pack partially changed");
            });
            Test("file lock during activation rolls back previously moved jars", root => {
                string first = Jar(root, "a.jar", "a"); string second = Jar(root, "b.jar", "b");
                string folder = Path.Combine(root, "mods"); Directory.CreateDirectory(folder);
                string currentA = Path.Combine(folder, "a.jar"), currentB = Path.Combine(folder, "b.jar");
                File.WriteAllText(currentA, "old a"); File.WriteAllText(currentB, "old b");
                using (var locked = new FileStream(currentB, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    Fails(() => ClientMods.Repair(folder, Manifest(first, second), _ => {}, (url, dest) => File.Copy(url.EndsWith("a.jar") ? first : second, dest)));
                }
                Assert(File.ReadAllText(currentA) == "old a" && File.ReadAllText(currentB) == "old b", "Rollback did not restore files");
            });
            Test("concurrent repair rejected before modifications", root => {
                string jar = Jar(root, "a.jar", "a"); string folder = Path.Combine(root, "mods"); Directory.CreateDirectory(folder);
                using (var guard = new FileStream(Path.Combine(folder, ".primordial-mods.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
                    Fails(() => ClientMods.Repair(folder, Manifest(jar), _ => {}, (url, dest) => { throw new Exception("Must not download"); }));
                }
                Assert(!Directory.GetFiles(folder, "*.jar").Any(), "Concurrent repair modified mods");
            });
            Test("malformed manifest, duplicate names and unsafe URL rejected", root => {
                string jar = Jar(root, "a.jar", "a"); var manifest = Manifest(jar);
                manifest.mods[0].filename = "../a.jar";
                Fails(() => ClientMods.Repair(Path.Combine(root,"mods"), manifest, _ => {}, (url,dest) => {}));
                Assert(!Directory.Exists(Path.Combine(root,"mods")), "Invalid manifest created folder");
                manifest = Manifest(jar, jar); Fails(() => ClientMods.Parse(new JavaScriptSerializer().Serialize(manifest)));
                manifest = Manifest(jar); manifest.mods[0].url = "https://example.com/a.jar";
                Fails(() => ClientMods.Parse(new JavaScriptSerializer().Serialize(manifest)));
            });
            Test("legacy bridge preserved only from package checksum manifest", root => {
                string jar = Jar(root, "a.jar", "a"); var manifest = Manifest(jar);
                var package = new PackageManifest { files = new [] { new PackageFile {
                    path="launcher/instances/Primordial-Adventures/.minecraft/mods/primordial-bridge.jar", sha256=new string('a',64)
                } } };
                File.WriteAllText(Path.Combine(root,"package-files.json"), new JavaScriptSerializer().Serialize(package));
                manifest = ClientMods.LegacyBundle(manifest, root);
                Assert(manifest.mods.Length == 2 && manifest.mods[1].url == null, "Bridge not preserved");
            });
            Test("Minecraft and NeoForge version mismatch blocks launch", root => {
                string path = Path.Combine(root, "mmc-pack.json");
                File.WriteAllText(path, "{\"components\":[{\"uid\":\"net.minecraft\",\"version\":\"1.21.1\"},{\"uid\":\"net.neoforged\",\"version\":\"21.1.252\"}]}");
                ClientMods.VerifyGameVersion(root, embedded);
                File.WriteAllText(path, File.ReadAllText(path).Replace("21.1.252", "21.1.1"));
                Fails(() => ClientMods.VerifyGameVersion(root, embedded));
            });
            Console.WriteLine(passed + " regression scenarios passed."); return 0;
        } catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
