using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace PrimordialLauncher {
    public partial class LauncherForm : Form {
        public const string VERSION = "1.2.0-dev";
        private const string SERVER_HOST = "mc.primordial.my";
        private const int SERVER_PORT = 25565;
        private const string CONFIG_FILE = "launcher_config.ini";

        // UI Controls - Navigation
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Button btnNavPlay;
        private Button btnNavSkins;
        private Button btnNavMods;
        private Button btnNavSettings;
        private Button btnNavHelp;

        // UI Panels for Tabs
        private Panel tabPlay;
        private Panel tabSkins;
        private Panel tabMods;
        private Panel tabSettings;
        private Panel tabHelp;

        // Skin Badge Controls
        private Panel pnlSkinBadge;
        private PictureBox pbBadgeThumb;
        private Label lblBadgeSkinName;

        // Tab Play Controls
        private Label lblServerTitle;
        private Label lblServerStatus;
        private Label lblServerPing;
        private TextBox txtNickname;
        private RadioButton rbRam4G;
        private RadioButton rbRam6G;
        private RadioButton rbRam8G;
        private Button btnPlayAction;
        private ProgressBar prgAction;
        private Label lblActionStatus;
        private ListBox lstLog;
        private Label lblGameSession;

        // Tab Settings Controls
        private NumericUpDown numRam;
        private CheckBox chkKeepOpen;

        // State
        private string baseInstallPath;
        private bool isGameInstalled = false;
        private bool isGameRunning = false;
        private bool isDownloading = false;
        private Process gameProcess;
        private System.Windows.Forms.Timer sessionTimer;
        private DateTime sessionStartTime;

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
            BuildAuthenticationControls();
            InitActiveSkinFromPreferences();
            txtNickname.TextChanged += delegate { InitActiveSkinFromPreferences(); };
            this.Shown += delegate { CheckServerStatusAsync(); };
        }

        private void InitUI() {
            this.Text = "Primordial Adventures Launcher " + VERSION;
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new Size(800, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(14, 16, 23); // Deep space dark
            this.ForeColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Left Sidebar
            pnlSidebar = new Panel();
            pnlSidebar.Location = new Point(0, 0);
            pnlSidebar.Size = new Size(200, 580);
            pnlSidebar.BackColor = Color.FromArgb(20, 23, 34);
            this.Controls.Add(pnlSidebar);

            // Brand in Sidebar
            Label lblBrand = new Label();
            lblBrand.Text = "⚔️ PRIMORDIAL";
            lblBrand.Font = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            lblBrand.ForeColor = Color.FromArgb(16, 185, 129); // Emerald
            lblBrand.Location = new Point(16, 22);
            lblBrand.AutoSize = true;
            pnlSidebar.Controls.Add(lblBrand);

            Label lblVersion = new Label();
            lblVersion.Text = "NeoForge 1.21.1";
            lblVersion.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblVersion.ForeColor = Color.FromArgb(148, 163, 184);
            lblVersion.Location = new Point(18, 48);
            lblVersion.AutoSize = true;
            pnlSidebar.Controls.Add(lblVersion);

            // Navigation Buttons
            int btnY = 95;
            btnNavPlay = CreateNavButton("🎮  Play Game", btnY);
            btnNavSkins = CreateNavButton("🎨  Custom Skins", 141);
            btnNavMods = CreateNavButton("📦  Modpack (32)", 187);
            btnNavSettings = CreateNavButton("⚙️  Settings & RAM", 233);
            btnNavHelp = CreateNavButton("📖  Guide & Tips", 279);

            btnNavPlay.Click += delegate { SwitchTab(tabPlay, btnNavPlay); };
            btnNavSkins.Click += delegate { SwitchTab(tabSkins, btnNavSkins); };
            btnNavMods.Click += delegate { SwitchTab(tabMods, btnNavMods); };
            btnNavSettings.Click += delegate { SwitchTab(tabSettings, btnNavSettings); };
            btnNavHelp.Click += delegate { SwitchTab(tabHelp, btnNavHelp); };

            pnlSidebar.Controls.Add(btnNavPlay);
            pnlSidebar.Controls.Add(btnNavSkins);
            pnlSidebar.Controls.Add(btnNavMods);
            pnlSidebar.Controls.Add(btnNavSettings);
            pnlSidebar.Controls.Add(btnNavHelp);

            // Main Content Area
            pnlContent = new Panel();
            pnlContent.Location = new Point(200, 0);
            pnlContent.Size = new Size(600, 580);
            pnlContent.BackColor = Color.FromArgb(14, 16, 23);
            this.Controls.Add(pnlContent);

            // Initialize Tabs
            BuildPlayTab();
            BuildSkinsTab();
            BuildModsTab();
            BuildSettingsTab();
            BuildHelpTab();

            // Default to Play tab
            SwitchTab(tabPlay, btnNavPlay);

            // Session duration timer
            sessionTimer = new System.Windows.Forms.Timer();
            sessionTimer.Interval = 1000;
            sessionTimer.Tick += delegate {
                if (isGameRunning) {
                    TimeSpan span = DateTime.Now - sessionStartTime;
                    lblGameSession.Text = string.Format("🎮 Game running ({0:D2}:{1:D2}:{2:D2})", span.Hours, span.Minutes, span.Seconds);
                }
            };
        }

        private Button CreateNavButton(string text, int top) {
            Button btn = new Button();
            btn.Text = text;
            btn.UseMnemonic = false;
            btn.Location = new Point(10, top);
            btn.Size = new Size(180, 40);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(12, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.FromArgb(203, 213, 225);
            return btn;
        }

        private void SwitchTab(Panel selectedTab, Button activeNavBtn) {
            tabPlay.Visible = (selectedTab == tabPlay);
            tabSkins.Visible = (selectedTab == tabSkins);
            tabMods.Visible = (selectedTab == tabMods);
            tabSettings.Visible = (selectedTab == tabSettings);
            tabHelp.Visible = (selectedTab == tabHelp);

            Button[] btns = new Button[] { btnNavPlay, btnNavSkins, btnNavMods, btnNavSettings, btnNavHelp };
            foreach (Button b in btns) {
                if (b == activeNavBtn) {
                    b.BackColor = Color.FromArgb(30, 41, 59);
                    b.ForeColor = Color.FromArgb(52, 211, 153);
                } else {
                    b.BackColor = Color.Transparent;
                    b.ForeColor = Color.FromArgb(203, 213, 225);
                }
            }
        }

        #region Tab 1: Play Tab
        private void BuildPlayTab() {
            tabPlay = new Panel();
            tabPlay.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(tabPlay);

            // Server Card
            Panel pnlServer = new Panel();
            pnlServer.Location = new Point(25, 20);
            pnlServer.Size = new Size(550, 75);
            pnlServer.BackColor = Color.FromArgb(22, 26, 38);
            pnlServer.Paint += delegate(object s, PaintEventArgs e) {
                using (Pen p = new Pen(Color.FromArgb(40, 46, 68), 1)) {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlServer.Width - 1, pnlServer.Height - 1);
                }
            };
            tabPlay.Controls.Add(pnlServer);

            lblServerTitle = new Label();
            lblServerTitle.Text = "Server: mc.primordial.my";
            lblServerTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblServerTitle.ForeColor = Color.White;
            lblServerTitle.Location = new Point(16, 14);
            lblServerTitle.AutoSize = true;
            pnlServer.Controls.Add(lblServerTitle);

            lblServerStatus = new Label();
            lblServerStatus.Text = "⚪ Checking server ping...";
            lblServerStatus.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblServerStatus.ForeColor = Color.FromArgb(203, 213, 225);
            lblServerStatus.Location = new Point(16, 42);
            lblServerStatus.AutoSize = true;
            pnlServer.Controls.Add(lblServerStatus);

            lblServerPing = new Label();
            lblServerPing.Text = "";
            lblServerPing.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblServerPing.ForeColor = Color.FromArgb(148, 163, 184);
            lblServerPing.Location = new Point(430, 42);
            lblServerPing.Size = new Size(100, 20);
            lblServerPing.TextAlign = ContentAlignment.MiddleRight;
            pnlServer.Controls.Add(lblServerPing);

            // Player Profile Input
            Label lblNickTitle = new Label();
            lblNickTitle.Text = "Player Nickname:";
            lblNickTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblNickTitle.ForeColor = Color.FromArgb(226, 232, 240);
            lblNickTitle.Location = new Point(25, 110);
            lblNickTitle.AutoSize = true;
            tabPlay.Controls.Add(lblNickTitle);

            txtNickname = new TextBox();
            txtNickname.Font = new Font("Segoe UI", 11f, FontStyle.Regular);
            txtNickname.Location = new Point(25, 136);
            txtNickname.Size = new Size(270, 27);
            txtNickname.BackColor = Color.FromArgb(28, 33, 48);
            txtNickname.ForeColor = Color.White;
            txtNickname.BorderStyle = BorderStyle.FixedSingle;
            tabPlay.Controls.Add(txtNickname);

            // RAM Allocation Radio Group
            Label lblRamTitle = new Label();
            lblRamTitle.Text = "Allocated RAM:";
            lblRamTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblRamTitle.ForeColor = Color.FromArgb(226, 232, 240);
            lblRamTitle.Location = new Point(325, 110);
            lblRamTitle.AutoSize = true;
            tabPlay.Controls.Add(lblRamTitle);

            rbRam4G = new RadioButton();
            rbRam4G.Text = "4 GB";
            rbRam4G.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            rbRam4G.Location = new Point(325, 138);
            rbRam4G.AutoSize = true;
            rbRam4G.Checked = true;
            tabPlay.Controls.Add(rbRam4G);

            rbRam6G = new RadioButton();
            rbRam6G.Text = "6 GB";
            rbRam6G.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            rbRam6G.Location = new Point(400, 138);
            rbRam6G.AutoSize = true;
            tabPlay.Controls.Add(rbRam6G);

            rbRam8G = new RadioButton();
            rbRam8G.Text = "8 GB";
            rbRam8G.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            rbRam8G.Location = new Point(475, 138);
            rbRam8G.AutoSize = true;
            tabPlay.Controls.Add(rbRam8G);

            // Active Skin Badge Control
            BuildSkinBadgeControl();

            // Progress bar
            prgAction = new ProgressBar();
            prgAction.Location = new Point(25, 212);
            prgAction.Size = new Size(550, 4);
            prgAction.Visible = false;
            tabPlay.Controls.Add(prgAction);

            lblActionStatus = new Label();
            lblActionStatus.Location = new Point(25, 212);
            lblActionStatus.Size = new Size(550, 18);
            lblActionStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblActionStatus.ForeColor = Color.FromArgb(148, 163, 184);
            lblActionStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblActionStatus.Visible = false;
            tabPlay.Controls.Add(lblActionStatus);

            // Big Play Button
            btnPlayAction = new Button();
            btnPlayAction.Location = new Point(25, 218);
            btnPlayAction.Size = new Size(550, 50);
            btnPlayAction.FlatStyle = FlatStyle.Flat;
            btnPlayAction.FlatAppearance.BorderSize = 0;
            btnPlayAction.Font = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            btnPlayAction.Cursor = Cursors.Hand;
            btnPlayAction.Click += BtnPlayAction_Click;
            tabPlay.Controls.Add(btnPlayAction);

            // Session duration label
            lblGameSession = new Label();
            lblGameSession.Location = new Point(25, 273);
            lblGameSession.Size = new Size(550, 20);
            lblGameSession.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblGameSession.ForeColor = Color.FromArgb(52, 211, 153);
            lblGameSession.TextAlign = ContentAlignment.MiddleCenter;
            lblGameSession.Visible = false;
            tabPlay.Controls.Add(lblGameSession);

            // Live Launch Progress Console / Log List
            Label lblLogTitle = new Label();
            lblLogTitle.Text = "Launch Milestones & Logs:";
            lblLogTitle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblLogTitle.ForeColor = Color.FromArgb(148, 163, 184);
            lblLogTitle.Location = new Point(25, 298);
            lblLogTitle.AutoSize = true;
            tabPlay.Controls.Add(lblLogTitle);

            lstLog = new ListBox();
            lstLog.Location = new Point(25, 322);
            lstLog.Size = new Size(550, 205);
            lstLog.BackColor = Color.FromArgb(19, 21, 31);
            lstLog.ForeColor = Color.FromArgb(203, 213, 225);
            lstLog.BorderStyle = BorderStyle.FixedSingle;
            lstLog.Font = new Font("Consolas", 8.5f, FontStyle.Regular);
            tabPlay.Controls.Add(lstLog);

            LogMessage("Launcher initialized. Ready to launch.");
        }
        #endregion

        #region Tab 2: Modpack Tab
        private void BuildModsTab() {
            tabMods = new Panel();
            tabMods.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(tabMods);

            Label lblModsHeader = new Label();
            lblModsHeader.Text = "Included Adventure Mods (32 Total)";
            lblModsHeader.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblModsHeader.ForeColor = Color.FromArgb(16, 185, 129);
            lblModsHeader.Location = new Point(25, 20);
            lblModsHeader.AutoSize = true;
            tabMods.Controls.Add(lblModsHeader);

            ListView lv = new ListView();
            lv.Location = new Point(25, 60);
            lv.Size = new Size(550, 485);
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.BackColor = Color.FromArgb(20, 23, 34);
            lv.ForeColor = Color.White;
            lv.BorderStyle = BorderStyle.FixedSingle;
            lv.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            lv.Columns.Add("Mod Name", 195);
            lv.Columns.Add("Category", 100);
            lv.Columns.Add("Description", 250);

            AddModItem(lv, "Iron's Spells 'n Spellbooks", "Magic", "10+ magic schools, spellbooks, mana progression");
            AddModItem(lv, "Simply Swords", "Combat", "Katanas, spears, halberds, unique rare weapons");
            AddModItem(lv, "Better Combat", "Combat", "Attack animations, combos, dual wielding");
            AddModItem(lv, "Combat Roll", "Combat", "Directional tactical dodge roll with cooldown");
            AddModItem(lv, "When Dungeons Arise", "World", "Massive rogue-like dungeons & sky temples");
            AddModItem(lv, "Lootr", "Multiplayer", "Individual personal chest loot for each player");
            AddModItem(lv, "Waystones", "Travel", "Teleportation monuments between home and points");
            AddModItem(lv, "You're in Grave Danger", "Survival", "Equipment stored safely in death graves");
            AddModItem(lv, "FTB Quests", "Quests", "15 optional beginner checklist guide quests");
            AddModItem(lv, "FTB Essentials", "Commands", "/home, /sethome, /spawn, /tpa, /god commands");
            AddModItem(lv, "FTB Teams", "Multiplayer", "Team management and party progression");
            AddModItem(lv, "NefAUTH", "Security", "In-game password protection for offline players");
            AddModItem(lv, "JEI (Just Enough Items)", "UI", "Item recipe search & crafting guide");
            AddModItem(lv, "Curios API", "Equipment", "Extra accessory and relic inventory slots");
            AddModItem(lv, "FerriteCore", "Performance", "Reduces Minecraft RAM usage significantly");
            AddModItem(lv, "ModernFix", "Performance", "Supercharged launch times & memory fixes");
            AddModItem(lv, "Simple Voice Chat", "Audio", "Proximity 3D positional voice chat (V)");
            AddModItem(lv, "JourneyMap", "Map & Radar", "Live real-time minimap, waypoints, friend radar (J)");
            AddModItem(lv, "Artifacts", "Exploration", "Rare accessories and baubles found in dungeon chests");
            AddModItem(lv, "Farmer's Delight", "Cooking", "Cooking pots, hearty meals, feasts, and knife slicing");
            AddModItem(lv, "SkinRestorer", "Visuals", "Change your in-game skin anytime via /skin command");
            AddModItem(lv, "Grappling Hook", "Movement", "Craft hooks and swing through the world");
            AddModItem(lv, "Primordial Integration", "Security & Skins", "Secure automatic login and synchronized bundled skins");
            AddModItem(lv, "Sodium / Embeddium", "Performance", "Next-gen graphics engine for ultra-high FPS");

            tabMods.Controls.Add(lv);
        }

        private void AddModItem(ListView lv, string name, string category, string desc) {
            ListViewItem item = new ListViewItem(name);
            item.SubItems.Add(category);
            item.SubItems.Add(desc);
            lv.Items.Add(item);
        }
        #endregion

        #region Tab 3: Settings Tab
        private void BuildSettingsTab() {
            tabSettings = new Panel();
            tabSettings.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(tabSettings);

            Label lblSetTitle = new Label();
            lblSetTitle.Text = "Launcher Settings & Utilities";
            lblSetTitle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblSetTitle.ForeColor = Color.FromArgb(16, 185, 129);
            lblSetTitle.Location = new Point(25, 20);
            lblSetTitle.AutoSize = true;
            tabSettings.Controls.Add(lblSetTitle);

            // RAM Memory Allocation Box
            GroupBox gbRam = new GroupBox();
            gbRam.Text = "Custom RAM Memory Allocation (MB)";
            gbRam.ForeColor = Color.FromArgb(203, 213, 225);
            gbRam.Location = new Point(25, 60);
            gbRam.Size = new Size(550, 80);
            tabSettings.Controls.Add(gbRam);

            numRam = new NumericUpDown();
            numRam.Minimum = 2048;
            numRam.Maximum = 16384;
            numRam.Increment = 1024;
            numRam.Value = 4096;
            numRam.Location = new Point(20, 32);
            numRam.Size = new Size(120, 26);
            numRam.BackColor = Color.FromArgb(28, 33, 48);
            numRam.ForeColor = Color.White;
            gbRam.Controls.Add(numRam);

            Label lblRamHelp = new Label();
            lblRamHelp.Text = "4096 MB (4 GB) is recommended. Use 6144 or 8192 for 16 GB+ PCs.";
            lblRamHelp.ForeColor = Color.FromArgb(148, 163, 184);
            lblRamHelp.Location = new Point(155, 34);
            lblRamHelp.AutoSize = true;
            gbRam.Controls.Add(lblRamHelp);

            // Behavior
            chkKeepOpen = new CheckBox();
            chkKeepOpen.Text = "Keep launcher open while playing (shows session time & status)";
            chkKeepOpen.Checked = true;
            chkKeepOpen.Location = new Point(25, 160);
            chkKeepOpen.AutoSize = true;
            tabSettings.Controls.Add(chkKeepOpen);

            // Utility Buttons
            Button btnOpenFolder = new Button();
            btnOpenFolder.Text = "📂 Open Game Folder";
            btnOpenFolder.Location = new Point(25, 210);
            btnOpenFolder.Size = new Size(265, 42);
            btnOpenFolder.FlatStyle = FlatStyle.Flat;
            btnOpenFolder.BackColor = Color.FromArgb(30, 41, 59);
            btnOpenFolder.ForeColor = Color.White;
            btnOpenFolder.Click += delegate {
                string folder = GetInstanceFolderPath();
                if (Directory.Exists(folder)) Process.Start("explorer.exe", folder);
                else MessageBox.Show("Game folder not found yet: " + folder, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            tabSettings.Controls.Add(btnOpenFolder);

            Button btnOpenLogs = new Button();
            btnOpenLogs.Text = "📋 View Latest Game Log";
            btnOpenLogs.Location = new Point(310, 210);
            btnOpenLogs.Size = new Size(265, 42);
            btnOpenLogs.FlatStyle = FlatStyle.Flat;
            btnOpenLogs.BackColor = Color.FromArgb(30, 41, 59);
            btnOpenLogs.ForeColor = Color.White;
            btnOpenLogs.Click += delegate {
                string logFile = Path.Combine(GetInstanceFolderPath(), ".minecraft", "logs", "latest.log");
                if (File.Exists(logFile)) Process.Start("notepad.exe", logFile);
                else MessageBox.Show("No log file found yet at:\n" + logFile, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            tabSettings.Controls.Add(btnOpenLogs);

            // Re-download / Repair button
            Button btnRepair = new Button();
            btnRepair.Text = "🔄 Re-download / Repair Game Files";
            btnRepair.Location = new Point(25, 270);
            btnRepair.Size = new Size(550, 42);
            btnRepair.FlatStyle = FlatStyle.Flat;
            btnRepair.BackColor = Color.FromArgb(51, 65, 85);
            btnRepair.ForeColor = Color.FromArgb(248, 113, 113);
            btnRepair.Click += delegate {
                DialogResult dr = MessageBox.Show("Do you want to re-download the game files?", "Repair Game", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes) {
                    SwitchTab(tabPlay, btnNavPlay);
                    StartGameDownload();
                }
            };
            tabSettings.Controls.Add(btnRepair);
        }
        #endregion

        #region Tab 4: Help Tab
        private void BuildHelpTab() {
            tabHelp = new Panel();
            tabHelp.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(tabHelp);

            Label lblHelpTitle = new Label();
            lblHelpTitle.Text = "In-Game Commands & Quick Guide";
            lblHelpTitle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblHelpTitle.ForeColor = Color.FromArgb(16, 185, 129);
            lblHelpTitle.Location = new Point(25, 20);
            lblHelpTitle.AutoSize = true;
            tabHelp.Controls.Add(lblHelpTitle);

            TextBox txtCheatSheet = new TextBox();
            txtCheatSheet.Multiline = true;
            txtCheatSheet.ReadOnly = true;
            txtCheatSheet.Location = new Point(25, 60);
            txtCheatSheet.Size = new Size(550, 485);
            txtCheatSheet.BackColor = Color.FromArgb(20, 23, 34);
            txtCheatSheet.ForeColor = Color.FromArgb(226, 232, 240);
            txtCheatSheet.Font = new Font("Consolas", 9.5f, FontStyle.Regular);
            txtCheatSheet.ScrollBars = ScrollBars.Vertical;
            txtCheatSheet.BorderStyle = BorderStyle.FixedSingle;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== SERVER & WHITELIST ===");
            sb.AppendLine("• Server IP: mc.primordial.my (Port: 25565)");
            sb.AppendLine("• Web Hub: https://minecraft.primordial.my");
            sb.AppendLine("• Ask the host to whitelist your exact nickname before signing in.");
            sb.AppendLine();
            sb.AppendLine("=== PROXIMITY VOICE CHAT ===");
            sb.AppendLine("• Press 'V' to open voice settings, volume & mic testing");
            sb.AppendLine("• Push-to-Talk or Voice Activation available in 'V' menu");
            sb.AppendLine("• Create private whisper groups with friends");
            sb.AppendLine();
            sb.AppendLine("=== CUSTOM SKINS (OFFLINE SUPPORT) ===");
            sb.AppendLine("• /skin <player>  : Copy any player's skin (e.g. /skin Technoblade)");
            sb.AppendLine("• /skin url <url> : Set custom skin from direct image link");
            sb.AppendLine("• /skin clear     : Reset to default character skin");
            sb.AppendLine();
            sb.AppendLine("=== AUTOMATIC SIGN-IN ===");
            sb.AppendLine("• Ask the host to approve your exact nickname.");
            sb.AppendLine("• Enter your password in the launcher; confirm it when claiming a new name.");
            sb.AppendLine("• Passwords are remembered securely for your Windows user.");
            sb.AppendLine("• Use Forget account to remove the saved password. Ask the host for a reset.");
            sb.AppendLine();
            sb.AppendLine("=== TRAVEL & BASE TELEPORTATION ===");
            sb.AppendLine("• /sethome <name> : Save your base location");
            sb.AppendLine("• /home <name>    : Teleport back to your base");
            sb.AppendLine("• /spawn          : Teleport to spawn point");
            sb.AppendLine("• /tpa <player>   : Request teleport to a friend");
            sb.AppendLine("• /tpaccept       : Accept a friend's teleport request");
            sb.AppendLine("• /back           : Return to location before teleport");
            sb.AppendLine();
            sb.AppendLine("=== COMBAT & CONTROLS ===");
            sb.AppendLine("• Dodge Roll: Press 'R' to roll in directional movement");
            sb.AppendLine("• Spells Menu: Open inventory or press 'V' for spell book");
            sb.AppendLine("• Crafting Recipes: Hover over items in JEI and press 'R'");
            sb.AppendLine("• Death Graves: Your gear is safely stored in a grave at death");
            sb.AppendLine("=== SECURITY & WHITELIST (ANTI-GRIEF) ===");
            sb.AppendLine("• Random players cannot join (strict Whitelist enforced).");
            sb.AppendLine("• Add friend: /whitelist add <nickname>");
            sb.AppendLine("• List friends: /whitelist list");
            sb.AppendLine("• Kick/Revoke: /whitelist remove <nickname>");
            sb.AppendLine("• Prevent fire grief: /gamerule doFireTick false");
            sb.AppendLine("• Prevent creeper grief: /gamerule mobGriefing false");
            sb.AppendLine();
            sb.AppendLine("=== OPERATOR PRIVILEGES (Primordial) ===");
            sb.AppendLine("• /god            : Toggle complete invulnerability");
            sb.AppendLine("• /fly            : Toggle flight in survival mode");
            sb.AppendLine("• /heal           : Restore full health and hunger");
            sb.AppendLine("• /gamemode creative : Switch to creative mode");

            txtCheatSheet.Text = sb.ToString();
            tabHelp.Controls.Add(txtCheatSheet);
        }
        #endregion

        #region Installation & Config Detection
        private void DetectInstallation() {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (File.Exists(Path.Combine(currentDir, "launcher", "elyprismlauncher.exe")) &&
                (Directory.Exists(Path.Combine(currentDir, "launcher", "instances", "Primordial-Adventures")) ||
                 Directory.Exists(Path.Combine(currentDir, "instances", "Primordial-Adventures")))) {
                baseInstallPath = currentDir;
                isGameInstalled = true;
                LogMessage("Detected portable installation in current directory.");
            }
            else if (File.Exists(Path.Combine(currentDir, "Primordial-Adventures-Portable", "launcher", "elyprismlauncher.exe")) &&
                     (Directory.Exists(Path.Combine(currentDir, "Primordial-Adventures-Portable", "launcher", "instances", "Primordial-Adventures")) ||
                      Directory.Exists(Path.Combine(currentDir, "Primordial-Adventures-Portable", "instances", "Primordial-Adventures")))) {
                baseInstallPath = Path.Combine(currentDir, "Primordial-Adventures-Portable");
                isGameInstalled = true;
                LogMessage("Detected installation in subfolder.");
            }
            else if (File.Exists(Path.Combine(localAppData, "PrimordialAdventures", "launcher", "elyprismlauncher.exe"))) {
                baseInstallPath = Path.Combine(localAppData, "PrimordialAdventures");
                isGameInstalled = true;
                LogMessage("Detected installation in LocalAppData.");
            }
            else {
                string globalAppdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string globalInstance = Path.Combine(globalAppdata, "ElyPrismLauncher", "instances", "Primordial-Adventures");
                string globalExe = Path.Combine(localAppData, "Programs", "ElyPrismLauncher", "elyprismlauncher.exe");
                if (Directory.Exists(globalInstance) && File.Exists(globalExe)) {
                    baseInstallPath = "GLOBAL";
                    isGameInstalled = true;
                    LogMessage("Detected installation in ElyPrismLauncher profile.");
                } else {
                    isGameInstalled = false;
                    LogMessage("Game files not detected. Click Download to install.");
                }
            }

            UpdateActionButton();
        }

        private string GetInstanceFolderPath() {
            if (baseInstallPath == "GLOBAL") {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ElyPrismLauncher", "instances", "Primordial-Adventures");
            } else if (!string.IsNullOrEmpty(baseInstallPath)) {
                return Path.Combine(baseInstallPath, "launcher", "instances", "Primordial-Adventures");
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private void UpdateActionButton() {
            if (isDownloading) {
                btnPlayAction.Enabled = false;
                btnPlayAction.Text = "⏳ DOWNLOADING GAME FILES...";
                btnPlayAction.BackColor = Color.FromArgb(75, 85, 99);
                btnPlayAction.ForeColor = Color.White;
            } else if (isGameRunning) {
                btnPlayAction.Enabled = true;
                btnPlayAction.Text = "🛑 STOP / KILL GAME PROCESS";
                btnPlayAction.BackColor = Color.FromArgb(239, 68, 68); // Red
                btnPlayAction.ForeColor = Color.White;
            } else if (isGameInstalled) {
                btnPlayAction.Enabled = true;
                btnPlayAction.Text = "🚀 PLAY NOW";
                btnPlayAction.BackColor = Color.FromArgb(16, 185, 129); // Emerald
                btnPlayAction.ForeColor = Color.White;
            } else {
                btnPlayAction.Enabled = true;
                btnPlayAction.Text = "📦 DOWNLOAD & INSTALL GAME (1.8 GB)";
                btnPlayAction.BackColor = Color.FromArgb(37, 99, 235); // Blue
                btnPlayAction.ForeColor = Color.White;
            }
        }

        private void LoadConfig() {
            try {
                string cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
                if (File.Exists(cfgPath)) {
                    string[] lines = File.ReadAllLines(cfgPath);
                    foreach (string line in lines) {
                        if (line.StartsWith("Nickname=")) txtNickname.Text = line.Substring("Nickname=".Length).Trim();
                        if (line.StartsWith("RAM=")) {
                            int r;
                            if (int.TryParse(line.Substring("RAM=".Length).Trim(), out r)) {
                                if (r == 6144) rbRam6G.Checked = true;
                                else if (r == 8192) rbRam8G.Checked = true;
                                else rbRam4G.Checked = true;
                                numRam.Value = r;
                            }
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
                int ramMb = GetSelectedRamMb();
                string cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
                File.WriteAllText(cfgPath, string.Format("Nickname={0}\nRAM={1}\n", txtNickname.Text.Trim(), ramMb));
            } catch { }
        }

        private int GetSelectedRamMb() {
            if (rbRam8G.Checked) return 8192;
            if (rbRam6G.Checked) return 6144;
            return 4096;
        }

        private void LogMessage(string msg) {
            string line = string.Format("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), msg);
            lstLog.Items.Add(line);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }
        #endregion

        #region Server Ping Check
        private void CheckServerStatusAsync() {
            ThreadPool.QueueUserWorkItem(delegate {
                bool online = false;
                long pingMs = 0;
                Stopwatch sw = Stopwatch.StartNew();

                try {
                    using (TcpClient tcp = new TcpClient()) {
                        IAsyncResult res = tcp.BeginConnect(SERVER_HOST, SERVER_PORT, null, null);
                        bool success = res.AsyncWaitHandle.WaitOne(3000);
                        if (success && tcp.Connected) {
                            tcp.EndConnect(res);
                            sw.Stop();
                            pingMs = sw.ElapsedMilliseconds;
                            online = true;
                        }
                    }
                } catch { }

                if(IsDisposed || !IsHandleCreated)return;
                this.BeginInvoke(new Action(delegate {
                    if (online) {
                        lblServerStatus.Text = string.Format("🟢 Online • mc.primordial.my");
                        lblServerStatus.ForeColor = Color.FromArgb(52, 211, 153);
                        lblServerPing.Text = string.Format("{0} ms", pingMs);
                        LogMessage(string.Format("Server ping verified: {0} ms", pingMs));
                    } else {
                        lblServerStatus.Text = "🔴 Offline / Standby";
                        lblServerStatus.ForeColor = Color.FromArgb(248, 113, 113);
                        lblServerPing.Text = "";
                        LogMessage("Server mc.primordial.my is not reachable or still booting.");
                    }
                }));
            });
        }
        #endregion

        #region Play & Download Actions
        private void BtnPlayAction_Click(object sender, EventArgs e) {
            if (isGameRunning) {
                // Kill process
                try {
                    if (gameProcess != null && !gameProcess.HasExited) {
                        gameProcess.Kill();
                        LogMessage("Game process terminated by user.");
                    }
                } catch { }
                return;
            }

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
                BeginAuthentication(nick);
            }
        }

        private void StartGameDownload() {
            foreach (Process existing in Process.GetProcessesByName("elyprismlauncher")) {
                existing.Dispose();
                MessageBox.Show("Close PineconeMC and Minecraft before repairing the installation.");
                return;
            }
            isDownloading = true;
            UpdateActionButton();
            prgAction.Visible = true;
            prgAction.Value = 0;
            lblActionStatus.Visible = true;
            lblActionStatus.Text = "Checking the current release...";
            ThreadPool.QueueUserWorkItem(delegate {
                string zip = Path.Combine(Path.GetTempPath(), "Primordial-" + Guid.NewGuid().ToString("N") + ".partial");
                try {
                    ReleaseManifest release = ReleaseInstaller.GetRelease();
                    ReleaseAsset asset = release.assets["Primordial-Adventures-Portable.zip"];
                    string target = isGameInstalled && baseInstallPath != "GLOBAL" ? baseInstallPath : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimordialAdventures");
                    using (var client = new WebClient()) {
                        client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs args) {
                            if (!IsDisposed) BeginInvoke(new Action(delegate { prgAction.Value = args.ProgressPercentage; lblActionStatus.Text = "Downloading " + release.version + ": " + args.ProgressPercentage + "%"; }));
                        };
                        client.DownloadFileTaskAsync(new Uri(asset.url),zip).GetAwaiter().GetResult();
                    }
                    BeginInvoke(new Action(delegate { lblActionStatus.Text = "Verifying and installing game files..."; }));
                    ReleaseInstaller.Install(zip,target,asset,release.version);
                    baseInstallPath=target;
                    isGameInstalled=true;
                    BeginInvoke(new Action(delegate { LogMessage("Installed release " + release.version + ". Ready to play."); }));
                } catch(Exception error) {
                    if(!IsDisposed) BeginInvoke(new Action(delegate { LogMessage("Installation failed: " + error.Message); MessageBox.Show("Installation failed. You can retry safely.\n\n" + error.Message); }));
                } finally {
                    if(File.Exists(zip))File.Delete(zip);
                    if(!IsDisposed)BeginInvoke(new Action(delegate { isDownloading=false; prgAction.Visible=false; lblActionStatus.Visible=false; UpdateActionButton(); }));
                }
            });
        }

        private static void SetConfigValue(string path, string key, string value) {
            string content = File.ReadAllText(path);
            string pattern = "(?m)^" + Regex.Escape(key) + "=[^\r\n]*";
            if(Regex.IsMatch(content,pattern))content=Regex.Replace(content,pattern,delegate(Match match){return key+"="+value;});
            else {
                Match section=Regex.Match(content,@"(?m)^\[General\]\r?\n");
                if(!section.Success)throw new InvalidDataException("Instance settings are missing the General section.");
                content=content.Insert(section.Index+section.Length,key+"="+value+"\r\n");
            }
            File.WriteAllText(path, content);
        }

        public static void WriteOfflineAccount(string path, string nickname) {
            string skinUrl = null;
            try {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string prefDir = Path.Combine(localAppData, "PrimordialAdventures");
                string key = string.IsNullOrEmpty(nickname) ? "default" : nickname.Trim();
                string nickFile = Path.Combine(prefDir, key + ".skin");
                string skinId = null;
                if (File.Exists(nickFile)) {
                    skinId = File.ReadAllText(nickFile).Trim();
                } else {
                    string jsonFile = Path.Combine(prefDir, "skin_preferences.json");
                    if (File.Exists(jsonFile)) {
                        var ser = new JavaScriptSerializer();
                        var dict = ser.Deserialize<Dictionary<string, string>>(File.ReadAllText(jsonFile));
                        if (dict != null && dict.ContainsKey(key)) skinId = dict[key];
                    }
                }
                if (!string.IsNullOrEmpty(skinId)) {
                    skinUrl = "skins/" + skinId + ".png";
                }
            } catch { }
            WriteOfflineAccount(path, nickname, skinUrl, "classic");
        }

        public static void WriteOfflineAccount(string path, string nickname, string skinUrl, string skinModel) {
            if (!Regex.IsMatch(nickname, @"^[A-Za-z0-9_]{3,16}$"))
                throw new ArgumentException("Invalid Minecraft nickname.");
            var serializer = new JavaScriptSerializer();
            var root = File.Exists(path) ? serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(path)) : new Dictionary<string, object>();
            var accounts = new List<object>();
            object oldAccounts;
            if (root.TryGetValue("accounts", out oldAccounts)) {
                foreach (Dictionary<string, object> account in (IEnumerable)oldAccounts) {
                    account["active"] = false;
                    var profile = account.ContainsKey("profile") ? account["profile"] as Dictionary<string, object> : null;
                    if (Convert.ToString(account["type"]) == "Offline" &&
                        (profile == null || Convert.ToString(profile["name"]) == nickname)) continue;
                    accounts.Add(account);
                }
            }
            byte[] digest;
            using (var md5 = MD5.Create()) digest = md5.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + nickname));
            digest[6] = (byte)((digest[6] & 15) | 48);
            digest[8] = (byte)((digest[8] & 63) | 128);
            string uuid = BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();

            string skinUrlValue = !string.IsNullOrEmpty(skinUrl) ? skinUrl : "";
            string skinModelValue = !string.IsNullOrEmpty(skinModel) ? skinModel : "classic";
            accounts.Add(new {
                active = true, type = "Offline",
                entitlement = new { canPlayMinecraft = true, ownsMinecraft = true },
                profile = new { id = uuid, name = nickname, capes = new object[0], skin = new { id = "", url = skinUrlValue, variant = skinModelValue, model = skinModelValue } },
                ygg = new { token = "0", iat = 0, extra = new { clientToken = uuid, userName = nickname } }
            });
            root["accounts"] = accounts;
            root["formatVersion"] = 3;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".primordial.tmp";
            File.WriteAllText(temp, serializer.Serialize(root), new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, path + ".primordial.bak");
            else File.Move(temp, path);
        }

        private void LaunchGame(string nickname) {
            try {
                LogMessage("============================================");
                LogMessage(string.Format("[1/4] Preparing player profile for '{0}'...", nickname));

                string launcherExe;
                string accountsJson;
                string instanceCfg = null;
                int ramMb = GetSelectedRamMb();

                if (baseInstallPath == "GLOBAL") {
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    launcherExe = Path.Combine(localAppData, "Programs", "ElyPrismLauncher", "elyprismlauncher.exe");
                    accountsJson = Path.Combine(appData, "ElyPrismLauncher", "accounts.json");
                    instanceCfg = Path.Combine(appData, "ElyPrismLauncher", "instances", "Primordial-Adventures", "instance.cfg");
                } else {
                    launcherExe = Path.Combine(baseInstallPath, "launcher", "elyprismlauncher.exe");
                    accountsJson = Path.Combine(baseInstallPath, "launcher", "accounts.json");
                    string[] targetCfgs = new string[] {
                        Path.Combine(baseInstallPath, "launcher", "instances", "Primordial-Adventures", "instance.cfg"),
                        Path.Combine(baseInstallPath, "instances", "Primordial-Adventures", "instance.cfg")
                    };

                    // Set JavaPath and memory in instance.cfg
                    LogMessage("[2/4] Configuring Java 21 runtime...");
                    string bundledJava = Path.Combine(baseInstallPath, "java", "jdk-21.0.12.1+1", "bin", "javaw.exe").Replace('\\', '/');
                    foreach (string targetCfg in targetCfgs) {
                        if (File.Exists(targetCfg)) {
                            string cfgContent = File.ReadAllText(targetCfg);
                            string[] lines = cfgContent.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                            for (int i = 0; i < lines.Length; i++) {
                                if (lines[i].StartsWith("JavaPath=")) lines[i] = "JavaPath=" + bundledJava;
                                if (lines[i].StartsWith("OverrideJavaLocation=")) lines[i] = "OverrideJavaLocation=true";
                                if (lines[i].StartsWith("MaxMemAlloc=")) lines[i] = "MaxMemAlloc=" + ramMb;
                            }
                            File.WriteAllText(targetCfg, string.Join("\r\n", lines));
                        }
                    }
                }

                if (!Regex.IsMatch(nickname, @"^[A-Za-z0-9_]{3,16}$"))
                    throw new ArgumentException("Use a nickname of 3-16 letters, numbers or underscores.");
                foreach (Process existing in Process.GetProcessesByName("elyprismlauncher")) {
                    existing.Dispose();
                    throw new InvalidOperationException("Close PineconeMC first, then press Play again so it can load your selected profile.");
                }
                LogMessage(string.Format("[3/4] Preparing offline profile for '{0}' (RAM: {1} MB)...", nickname, ramMb));
                string skinRel = GetActiveSkinRelativePath();
                WriteOfflineAccount(accountsJson, nickname, skinRel, "classic");
                string activeInstanceCfg=Path.Combine(GetInstanceFolderPath(),"instance.cfg");
                SetConfigValue(activeInstanceCfg,"OverrideJavaArgs","true");
                SetConfigValue(activeInstanceCfg,"JvmArgs","-XX:+UseG1GC");
                SetConfigValue(activeInstanceCfg,"UseOptimizedJvmArgs","false");
                SetConfigValue(activeInstanceCfg,"GarbageCollectorPreset","None");
                if (baseInstallPath == "GLOBAL") {
                    SetConfigValue(instanceCfg, "OverrideMemory", "true");
                    SetConfigValue(instanceCfg, "MaxMemAlloc", ramMb.ToString());
                }

                WriteSessionHandoff();
                // The game consumes a short-lived proof ticket, never a password.
                LogMessage("[4/4] Launching NeoForge 1.21.1 game engine...");
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = launcherExe;
                psi.Arguments = "--dir \""+Path.GetDirectoryName(accountsJson)+"\" --launch \"Primordial-Adventures\" --profile "+nickname+" --server "+SERVER_HOST+":"+SERVER_PORT;
                psi.WorkingDirectory = Path.GetDirectoryName(launcherExe);
                
                gameProcess = Process.Start(psi);
                if (gameProcess != null) {
                    gameProcess.EnableRaisingEvents = true;
                    gameProcess.Exited += GameProcess_Exited;

                    isGameRunning = true;
                    sessionStartTime = DateTime.Now;
                    sessionTimer.Start();
                    lblGameSession.Visible = true;
                    UpdateActionButton();

                    LogMessage(string.Format("PineconeMC started (PID: {0}). Preparing Minecraft...", gameProcess.Id));
                    LogMessage("Startup progress and any Minecraft errors appear in PineconeMC.");

                    if (!chkKeepOpen.Checked) {
                        this.WindowState = FormWindowState.Minimized;
                    }
                }
            } catch (Exception ex) {
                LogMessage("ERROR: " + ex.Message);
                MessageBox.Show("Failed to launch game: " + ex.Message, "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isGameRunning = false;
                UpdateActionButton();
            }
        }

        private void GameProcess_Exited(object sender, EventArgs e) {
            this.BeginInvoke(new Action(delegate {
                sessionTimer.Stop();
                isGameRunning = false;
                lblGameSession.Visible = false;
                UpdateActionButton();

                int exitCode = 0;
                try { exitCode = gameProcess.ExitCode; } catch { }

                TimeSpan span = DateTime.Now - sessionStartTime;
                LogMessage(string.Format("PineconeMC process finished. Duration: {0:D2}m {1:D2}s.",
                    span.Minutes, span.Seconds));

                if (this.WindowState == FormWindowState.Minimized) {
                    this.WindowState = FormWindowState.Normal;
                    this.Activate();
                }

                if (exitCode != 0 && span.TotalSeconds < 5) {
                    LogMessage(string.Format("Notice: Process exited with code {0}.", exitCode));
                }
            }));
        }
        #endregion
    }
}
