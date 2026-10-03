using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace PrimordialLauncher {
    public class LauncherForm : Form {
        private const string RELEASE_ZIP_URL = "https://github.com/Aimen-B/primordial-minecraft-server/releases/download/v1.0.0/Primordial-Adventures-Portable.zip";
        private const string SERVER_HOST = "mc.primordial.my";
        private const int SERVER_PORT = 25565;
        private const string CONFIG_FILE = "launcher_config.ini";

        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblServerStatus;
        private Label lblNamePrompt;
        private TextBox txtNickname;
        private Button btnAction;
        private ProgressBar prgDownload;
        private Label lblProgressStatus;
        private Label lblServerIp;

        private string baseInstallPath;
        private bool isGameInstalled = false;
        private bool isDownloading = false;
        private WebClient webClient;

        [STAThread]
        public static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LauncherForm());
        }

        public LauncherForm() {
            InitUI();
            DetectInstallation();
            LoadConfig();
            CheckServerStatusAsync();
        }

        private void InitUI() {
            this.Text = "Primordial Adventures Launcher";
            this.Size = new Size(520, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(18, 19, 28);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Title
            lblTitle = new Label();
            lblTitle.Text = "PRIMORDIAL ADVENTURES";
            lblTitle.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(16, 185, 129); // Emerald
            lblTitle.Location = new Point(20, 25);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Subtitle
            lblSubtitle = new Label();
            lblSubtitle.Text = "Magic, Weapons & Exploration Modpack (NeoForge 1.21.1)";
            lblSubtitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblSubtitle.ForeColor = Color.FromArgb(156, 163, 175);
            lblSubtitle.Location = new Point(22, 62);
            lblSubtitle.AutoSize = true;
            this.Controls.Add(lblSubtitle);

            // Server Status Box
            Panel pnlStatus = new Panel();
            pnlStatus.BackColor = Color.FromArgb(28, 30, 45);
            pnlStatus.Location = new Point(25, 105);
            pnlStatus.Size = new Size(455, 60);
            pnlStatus.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(45, 49, 72), 1)) {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlStatus.Width - 1, pnlStatus.Height - 1);
                }
            };
            this.Controls.Add(pnlStatus);

            lblServerIp = new Label();
            lblServerIp.Text = "Server: mc.primordial.my";
            lblServerIp.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblServerIp.ForeColor = Color.White;
            lblServerIp.Location = new Point(15, 12);
            lblServerIp.AutoSize = true;
            pnlStatus.Controls.Add(lblServerIp);

            lblServerStatus = new Label();
            lblServerStatus.Text = "Checking status...";
            lblServerStatus.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblServerStatus.ForeColor = Color.FromArgb(234, 179, 8);
            lblServerStatus.Location = new Point(15, 34);
            lblServerStatus.AutoSize = true;
            pnlStatus.Controls.Add(lblServerStatus);

            // Nickname Prompt
            lblNamePrompt = new Label();
            lblNamePrompt.Text = "Enter Your Player Nickname:";
            lblNamePrompt.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblNamePrompt.ForeColor = Color.FromArgb(226, 232, 240);
            lblNamePrompt.Location = new Point(25, 190);
            lblNamePrompt.AutoSize = true;
            this.Controls.Add(lblNamePrompt);

            // Nickname Input Box
            txtNickname = new TextBox();
            txtNickname.Font = new Font("Segoe UI", 12f, FontStyle.Regular);
            txtNickname.Location = new Point(25, 220);
            txtNickname.Size = new Size(455, 32);
            txtNickname.BackColor = Color.FromArgb(28, 30, 45);
            txtNickname.ForeColor = Color.White;
            txtNickname.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtNickname);

            // Progress Bar (hidden by default)
            prgDownload = new ProgressBar();
            prgDownload.Location = new Point(25, 275);
            prgDownload.Size = new Size(455, 22);
            prgDownload.Visible = false;
            this.Controls.Add(prgDownload);

            // Status label for download
            lblProgressStatus = new Label();
            lblProgressStatus.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblProgressStatus.ForeColor = Color.FromArgb(148, 163, 184);
            lblProgressStatus.Location = new Point(25, 303);
            lblProgressStatus.Size = new Size(455, 25);
            lblProgressStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblProgressStatus.Visible = false;
            this.Controls.Add(lblProgressStatus);

            // Big Action Button
            btnAction = new Button();
            btnAction.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            btnAction.Location = new Point(25, 340);
            btnAction.Size = new Size(455, 55);
            btnAction.FlatStyle = FlatStyle.Flat;
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Cursor = Cursors.Hand;
            btnAction.Click += BtnAction_Click;
            this.Controls.Add(btnAction);

            // Footer note
            Label lblFooter = new Label();
            lblFooter.Text = "No Mojang account required • Password protected in-game";
            lblFooter.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblFooter.ForeColor = Color.FromArgb(100, 116, 139);
            lblFooter.Location = new Point(25, 405);
            lblFooter.Size = new Size(455, 20);
            lblFooter.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblFooter);
        }

        private void DetectInstallation() {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Check 1: Current folder is the extracted portable bundle
            if (File.Exists(Path.Combine(currentDir, "launcher", "elyprismlauncher.exe")) &&
                Directory.Exists(Path.Combine(currentDir, "instances", "Primordial-Adventures"))) {
                baseInstallPath = currentDir;
                isGameInstalled = true;
            }
            // Check 2: Subfolder in current directory
            else if (File.Exists(Path.Combine(currentDir, "Primordial-Adventures-Portable", "launcher", "elyprismlauncher.exe"))) {
                baseInstallPath = Path.Combine(currentDir, "Primordial-Adventures-Portable");
                isGameInstalled = true;
            }
            // Check 3: LocalAppData installed location
            else if (File.Exists(Path.Combine(localAppData, "PrimordialAdventures", "launcher", "elyprismlauncher.exe"))) {
                baseInstallPath = Path.Combine(localAppData, "PrimordialAdventures");
                isGameInstalled = true;
            }
            // Check 4: Global ElyPrismLauncher with Primordial-Adventures instance
            else {
                string globalAppdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string globalInstance = Path.Combine(globalAppdata, "ElyPrismLauncher", "instances", "Primordial-Adventures");
                string globalExe = Path.Combine(localAppData, "Programs", "ElyPrismLauncher", "elyprismlauncher.exe");
                if (Directory.Exists(globalInstance) && File.Exists(globalExe)) {
                    baseInstallPath = "GLOBAL";
                    isGameInstalled = true;
                }
            }

            UpdateActionButton();
        }

        private void UpdateActionButton() {
            if (isDownloading) {
                btnAction.Enabled = false;
                btnAction.Text = "INSTALLING...";
                btnAction.BackColor = Color.FromArgb(75, 85, 99);
                btnAction.ForeColor = Color.White;
            } else if (isGameInstalled) {
                btnAction.Enabled = true;
                btnAction.Text = "PLAY NOW";
                btnAction.BackColor = Color.FromArgb(16, 185, 129); // Emerald Green
                btnAction.ForeColor = Color.White;
            } else {
                btnAction.Enabled = true;
                btnAction.Text = "DOWNLOAD & INSTALL GAME (1.6 GB)";
                btnAction.BackColor = Color.FromArgb(37, 99, 235); // Blue
                btnAction.ForeColor = Color.White;
            }
        }

        private void LoadConfig() {
            try {
                string cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
                if (File.Exists(cfgPath)) {
                    string[] lines = File.ReadAllLines(cfgPath);
                    foreach (string line in lines) {
                        if (line.StartsWith("Nickname=")) {
                            txtNickname.Text = line.Substring("Nickname=".Length).Trim();
                        }
                    }
                }
            } catch { }

            if (string.IsNullOrEmpty(txtNickname.Text)) {
                txtNickname.Text = Environment.UserName;
            }
        }

        private void SaveConfig() {
            try {
                string cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
                File.WriteAllText(cfgPath, "Nickname=" + txtNickname.Text.Trim());
            } catch { }
        }

        private void CheckServerStatusAsync() {
            ThreadPool.QueueUserWorkItem(delegate {
                bool online = false;
                try {
                    using (TcpClient tcp = new TcpClient()) {
                        IAsyncResult res = tcp.BeginConnect(SERVER_HOST, SERVER_PORT, null, null);
                        bool success = res.AsyncWaitHandle.WaitOne(3000);
                        if (success && tcp.Connected) {
                            tcp.EndConnect(res);
                            online = true;
                        }
                    }
                } catch { }

                this.BeginInvoke(new Action(delegate {
                    if (online) {
                        lblServerStatus.Text = "[ONLINE] mc.primordial.my is active";
                        lblServerStatus.ForeColor = Color.FromArgb(52, 211, 153);
                    } else {
                        lblServerStatus.Text = "[OFFLINE/STANDBY] Server not reachable";
                        lblServerStatus.ForeColor = Color.FromArgb(248, 113, 113);
                    }
                }));
            });
        }

        private void BtnAction_Click(object sender, EventArgs e) {
            string nick = txtNickname.Text.Trim();
            if (string.IsNullOrEmpty(nick)) {
                MessageBox.Show("Please enter a player nickname first!", "Nickname Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNickname.Focus();
                return;
            }

            SaveConfig();

            if (!isGameInstalled) {
                StartGameDownload();
            } else {
                LaunchGame(nick);
            }
        }

        private void StartGameDownload() {
            isDownloading = true;
            UpdateActionButton();
            prgDownload.Visible = true;
            lblProgressStatus.Visible = true;
            lblProgressStatus.Text = "Connecting to GitHub Releases...";

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string installTarget = Path.Combine(localAppData, "PrimordialAdventures");
            string tempZip = Path.Combine(Path.GetTempPath(), "Primordial-Adventures-Portable.zip");

            webClient = new WebClient();
            webClient.DownloadProgressChanged += (s, e) => {
                prgDownload.Value = e.ProgressPercentage;
                double mbReceived = e.BytesReceived / 1048576.0;
                double mbTotal = e.TotalBytesToReceive / 1048576.0;
                lblProgressStatus.Text = string.Format("Downloading game files: {0}% ({1:F0} MB / {2:F0} MB)",
                    e.ProgressPercentage, mbReceived, mbTotal);
            };

            webClient.DownloadFileCompleted += (s, e) => {
                if (e.Error != null) {
                    MessageBox.Show("Download failed: " + e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isDownloading = false;
                    prgDownload.Visible = false;
                    lblProgressStatus.Visible = false;
                    UpdateActionButton();
                    return;
                }

                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        this.BeginInvoke(new Action(delegate {
                            lblProgressStatus.Text = "Extracting files (this takes ~30 seconds)...";
                        }));

                        if (!Directory.Exists(installTarget)) {
                            Directory.CreateDirectory(installTarget);
                        }

                        ZipFile.ExtractToDirectory(tempZip, installTarget);
                        File.Delete(tempZip);

                        baseInstallPath = installTarget;
                        isGameInstalled = true;
                        isDownloading = false;

                        this.BeginInvoke(new Action(delegate {
                            prgDownload.Visible = false;
                            lblProgressStatus.Visible = false;
                            UpdateActionButton();
                            MessageBox.Show("Installation Complete! Click PLAY NOW to start.", "Primordial Adventures", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    } catch (Exception ex) {
                        this.BeginInvoke(new Action(delegate {
                            MessageBox.Show("Extraction failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            isDownloading = false;
                            UpdateActionButton();
                        }));
                    }
                });
            };

            try {
                webClient.DownloadFileAsync(new Uri(RELEASE_ZIP_URL), tempZip);
            } catch (Exception ex) {
                MessageBox.Show("Failed to start download: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isDownloading = false;
                UpdateActionButton();
            }
        }

        private void LaunchGame(string nickname) {
            try {
                string launcherExe;
                string accountsJson;
                string instanceCfg;

                if (baseInstallPath == "GLOBAL") {
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    launcherExe = Path.Combine(localAppData, "Programs", "ElyPrismLauncher", "elyprismlauncher.exe");
                    accountsJson = Path.Combine(appData, "ElyPrismLauncher", "accounts.json");
                    instanceCfg = Path.Combine(appData, "ElyPrismLauncher", "instances", "Primordial-Adventures", "instance.cfg");
                } else {
                    launcherExe = Path.Combine(baseInstallPath, "launcher", "elyprismlauncher.exe");
                    accountsJson = Path.Combine(baseInstallPath, "launcher", "accounts.json");
                    instanceCfg = Path.Combine(baseInstallPath, "instances", "Primordial-Adventures", "instance.cfg");

                    // Ensure JavaPath is set correctly in instance.cfg
                    string bundledJava = Path.Combine(baseInstallPath, "java", "jdk-21.0.12.1+1", "bin", "javaw.exe").Replace('\\', '/');
                    if (File.Exists(instanceCfg)) {
                        string cfgContent = File.ReadAllText(instanceCfg);
                        if (cfgContent.Contains("JavaPath=")) {
                            string[] lines = cfgContent.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                            for (int i = 0; i < lines.Length; i++) {
                                if (lines[i].StartsWith("JavaPath=")) lines[i] = "JavaPath=" + bundledJava;
                            }
                            File.WriteAllText(instanceCfg, string.Join("\r\n", lines));
                        }
                    }
                }

                // Write offline account for nickname
                string uuid = Guid.NewGuid().ToString("N");
                string json = "{\n  \"accounts\": [\n    {\n      \"active\": true,\n      \"entitlement\": {\n        \"canPlayMinecraft\": true,\n        \"ownsMinecraft\": true\n      },\n      \"profile\": {\n        \"capes\": [],\n        \"id\": \"" + uuid + "\",\n        \"name\": \"" + nickname + "\"\n      },\n      \"type\": \"Offline\"\n    }\n  ],\n  \"formatVersion\": 3\n}";
                
                string accDir = Path.GetDirectoryName(accountsJson);
                if (!Directory.Exists(accDir)) Directory.CreateDirectory(accDir);
                File.WriteAllText(accountsJson, json, Encoding.UTF8);

                // Launch ElyPrism
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = launcherExe;
                psi.Arguments = "--launch \"Primordial-Adventures\" --server \"mc.primordial.my:25565\"";
                psi.WorkingDirectory = Path.GetDirectoryName(launcherExe);
                Process.Start(psi);

                // Exit launcher after starting game
                Thread.Sleep(1000);
                Application.Exit();
            } catch (Exception ex) {
                MessageBox.Show("Failed to launch game: " + ex.Message, "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
