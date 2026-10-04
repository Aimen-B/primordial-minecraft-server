using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PrimordialLauncher {
    public partial class LauncherForm : Form {
        // Whitelist UI Controls
        private Panel tabWhitelist;
        private TextBox txtWlFriendNick;
        private TextBox txtWlHostPassword;
        private Button btnWlAdd;
        private Label lblWlStatus;
        private ListBox lstWlPlayers;
        private Button btnWlRefresh;
        private Button btnWlRemove;

        private const string WhitelistApiUrl = "https://minecraft.primordial.my/api/whitelist";

        private void BuildWhitelistTab() {
            tabWhitelist = new Panel();
            tabWhitelist.Dock = DockStyle.Fill;
            tabWhitelist.Visible = false;
            pnlContent.Controls.Add(tabWhitelist);

            // Title Header
            Label lblHeader = new Label();
            lblHeader.Text = "🛡️ Host Whitelist Manager";
            lblHeader.Font = new Font("Segoe UI", 14.5f, FontStyle.Bold);
            lblHeader.ForeColor = Color.FromArgb(52, 211, 153); // Emerald
            lblHeader.Location = new Point(25, 18);
            lblHeader.AutoSize = true;
            tabWhitelist.Controls.Add(lblHeader);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Approve or remove friends in 1 click. Zero server commands or Dokploy required!";
            lblSubtitle.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184); // Muted slate
            lblSubtitle.Location = new Point(27, 48);
            lblSubtitle.AutoSize = true;
            tabWhitelist.Controls.Add(lblSubtitle);

            // Card 1: Add Friend Box (Size: 550x145)
            Panel pnlAddCard = new Panel();
            pnlAddCard.Location = new Point(25, 75);
            pnlAddCard.Size = new Size(550, 145);
            pnlAddCard.BackColor = Color.FromArgb(22, 26, 38);
            tabWhitelist.Controls.Add(pnlAddCard);

            Label lblCard1Title = new Label();
            lblCard1Title.Text = "➕ Whitelist a Friend";
            lblCard1Title.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblCard1Title.ForeColor = Color.FromArgb(241, 245, 249);
            lblCard1Title.Location = new Point(16, 12);
            lblCard1Title.AutoSize = true;
            pnlAddCard.Controls.Add(lblCard1Title);

            // Friend Nickname field
            Label lblNick = new Label();
            lblNick.Text = "Friend's Minecraft Nickname:";
            lblNick.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblNick.ForeColor = Color.FromArgb(148, 163, 184);
            lblNick.Location = new Point(16, 38);
            lblNick.AutoSize = true;
            pnlAddCard.Controls.Add(lblNick);

            txtWlFriendNick = new TextBox();
            txtWlFriendNick.Location = new Point(16, 58);
            txtWlFriendNick.Size = new Size(200, 26);
            txtWlFriendNick.BackColor = Color.FromArgb(28, 33, 48);
            txtWlFriendNick.ForeColor = Color.White;
            txtWlFriendNick.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            txtWlFriendNick.BorderStyle = BorderStyle.FixedSingle;
            pnlAddCard.Controls.Add(txtWlFriendNick);

            // Host Password field
            Label lblHostPass = new Label();
            lblHostPass.Text = "Host Password (auto-filled for Primordial):";
            lblHostPass.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblHostPass.ForeColor = Color.FromArgb(148, 163, 184);
            lblHostPass.Location = new Point(230, 38);
            lblHostPass.AutoSize = true;
            pnlAddCard.Controls.Add(lblHostPass);

            txtWlHostPassword = new TextBox();
            txtWlHostPassword.PasswordChar = '●';
            txtWlHostPassword.Location = new Point(230, 58);
            txtWlHostPassword.Size = new Size(170, 26);
            txtWlHostPassword.BackColor = Color.FromArgb(28, 33, 48);
            txtWlHostPassword.ForeColor = Color.White;
            txtWlHostPassword.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            txtWlHostPassword.BorderStyle = BorderStyle.FixedSingle;
            pnlAddCard.Controls.Add(txtWlHostPassword);

            // Add Button
            btnWlAdd = new Button();
            btnWlAdd.Text = "➕ Whitelist";
            btnWlAdd.Location = new Point(415, 56);
            btnWlAdd.Size = new Size(118, 29);
            btnWlAdd.FlatStyle = FlatStyle.Flat;
            btnWlAdd.FlatAppearance.BorderSize = 0;
            btnWlAdd.BackColor = Color.FromArgb(5, 150, 105); // Emerald Green
            btnWlAdd.ForeColor = Color.White;
            btnWlAdd.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnWlAdd.Cursor = Cursors.Hand;
            btnWlAdd.Click += delegate { ExecuteWhitelistAdd(); };
            pnlAddCard.Controls.Add(btnWlAdd);

            // Status feedback label
            lblWlStatus = new Label();
            lblWlStatus.Text = "Enter your friend's nickname and click Whitelist.";
            lblWlStatus.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            lblWlStatus.ForeColor = Color.FromArgb(56, 189, 248); // Sky blue
            lblWlStatus.Location = new Point(16, 98);
            lblWlStatus.Size = new Size(515, 38);
            pnlAddCard.Controls.Add(lblWlStatus);

            // Card 2: Current Whitelist (Size: 550x305)
            Panel pnlListCard = new Panel();
            pnlListCard.Location = new Point(25, 230);
            pnlListCard.Size = new Size(550, 305);
            pnlListCard.BackColor = Color.FromArgb(22, 26, 38);
            tabWhitelist.Controls.Add(pnlListCard);

            Label lblCard2Title = new Label();
            lblCard2Title.Text = "👥 Approved Whitelisted Players";
            lblCard2Title.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblCard2Title.ForeColor = Color.FromArgb(241, 245, 249);
            lblCard2Title.Location = new Point(16, 12);
            lblCard2Title.AutoSize = true;
            pnlListCard.Controls.Add(lblCard2Title);

            btnWlRefresh = new Button();
            btnWlRefresh.Text = "🔄 Refresh";
            btnWlRefresh.Location = new Point(320, 10);
            btnWlRefresh.Size = new Size(95, 26);
            btnWlRefresh.FlatStyle = FlatStyle.Flat;
            btnWlRefresh.FlatAppearance.BorderSize = 0;
            btnWlRefresh.BackColor = Color.FromArgb(30, 41, 59);
            btnWlRefresh.ForeColor = Color.FromArgb(226, 232, 240);
            btnWlRefresh.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnWlRefresh.Cursor = Cursors.Hand;
            btnWlRefresh.Click += delegate { ExecuteWhitelistList(); };
            pnlListCard.Controls.Add(btnWlRefresh);

            btnWlRemove = new Button();
            btnWlRemove.Text = "❌ Remove";
            btnWlRemove.Location = new Point(425, 10);
            btnWlRemove.Size = new Size(105, 26);
            btnWlRemove.FlatStyle = FlatStyle.Flat;
            btnWlRemove.FlatAppearance.BorderSize = 0;
            btnWlRemove.BackColor = Color.FromArgb(220, 38, 38); // Crimson
            btnWlRemove.ForeColor = Color.White;
            btnWlRemove.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnWlRemove.Cursor = Cursors.Hand;
            btnWlRemove.Click += delegate { ExecuteWhitelistRemove(); };
            pnlListCard.Controls.Add(btnWlRemove);

            lstWlPlayers = new ListBox();
            lstWlPlayers.Location = new Point(16, 44);
            lstWlPlayers.Size = new Size(515, 225);
            lstWlPlayers.BackColor = Color.FromArgb(14, 16, 23);
            lstWlPlayers.ForeColor = Color.FromArgb(241, 245, 249);
            lstWlPlayers.Font = new Font("Segoe UI", 10.0f, FontStyle.Regular);
            lstWlPlayers.BorderStyle = BorderStyle.None;
            lstWlPlayers.ItemHeight = 24;
            pnlListCard.Controls.Add(lstWlPlayers);

            Label lblNote = new Label();
            lblNote.Text = "Whitelisted players can join mc.primordial.my immediately with zero in-game registration.";
            lblNote.Font = new Font("Segoe UI", 8.0f, FontStyle.Italic);
            lblNote.ForeColor = Color.FromArgb(100, 116, 139);
            lblNote.Location = new Point(16, 278);
            lblNote.AutoSize = true;
            pnlListCard.Controls.Add(lblNote);
        }

        private void OnWhitelistTabOpened() {
            AutoFillHostPassword();
            ExecuteWhitelistList();
        }

        private void AutoFillHostPassword() {
            if (txtWlHostPassword == null) return;
            string currentNick = (txtNickname != null) ? txtNickname.Text.Trim() : "";
            
            // If already filled and not empty, keep it
            if (!string.IsNullOrEmpty(txtWlHostPassword.Text)) return;

            // Try loading remembered password for current user or primordial
            string pass = Credentials.Load(currentNick);
            if (string.IsNullOrEmpty(pass)) {
                pass = Credentials.Load("Primordial");
            }
            if (string.IsNullOrEmpty(pass)) {
                pass = Credentials.Load("primordial");
            }
            if (!string.IsNullOrEmpty(pass)) {
                txtWlHostPassword.Text = pass;
            } else if (currentNick.Equals("Primordial", StringComparison.OrdinalIgnoreCase)) {
                txtWlHostPassword.Text = "primordial2026";
            }
        }

        private string GetHostPassword() {
            if (txtWlHostPassword != null && !string.IsNullOrEmpty(txtWlHostPassword.Text)) {
                return txtWlHostPassword.Text.Trim();
            }
            string currentNick = (txtNickname != null) ? txtNickname.Text.Trim() : "";
            string pass = Credentials.Load(currentNick);
            if (string.IsNullOrEmpty(pass)) {
                pass = Credentials.Load("Primordial");
            }
            return !string.IsNullOrEmpty(pass) ? pass : "primordial2026";
        }

        private void ExecuteWhitelistAdd() {
            string friend = (txtWlFriendNick != null) ? txtWlFriendNick.Text.Trim() : "";
            if (string.IsNullOrEmpty(friend)) {
                ShowWhitelistStatus("⚠️ Please enter your friend's nickname.", false);
                return;
            }
            if (!Regex.IsMatch(friend, @"^[A-Za-z0-9_]{3,16}$")) {
                ShowWhitelistStatus("⚠️ Nickname must be 3-16 characters (letters, numbers, underscore).", false);
                return;
            }

            string password = GetHostPassword();
            ShowWhitelistStatus("⏳ Contacting server to whitelist " + friend + "...", null);
            btnWlAdd.Enabled = false;

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    var payload = new Dictionary<string, object> {
                        { "host", "Primordial" },
                        { "password", password },
                        { "action", "add" },
                        { "player", friend }
                    };
                    string responseJson = SendWhitelistRequest(payload);
                    var serializer = new JavaScriptSerializer();
                    var resp = serializer.Deserialize<Dictionary<string, object>>(responseJson);

                    bool success = resp != null && resp.ContainsKey("success") && Convert.ToBoolean(resp["success"]);
                    string msg = resp != null && resp.ContainsKey("message") ? Convert.ToString(resp["message"]) : "Done";

                    this.BeginInvoke((MethodInvoker)delegate {
                        btnWlAdd.Enabled = true;
                        if (success) {
                            ShowWhitelistStatus("✅ " + msg, true);
                            txtWlFriendNick.Text = "";
                            ExecuteWhitelistList();
                        } else {
                            ShowWhitelistStatus("❌ " + msg, false);
                        }
                    });
                } catch (Exception ex) {
                    this.BeginInvoke((MethodInvoker)delegate {
                        btnWlAdd.Enabled = true;
                        ShowWhitelistStatus("❌ Connection error: " + ex.Message, false);
                    });
                }
            });
        }

        private void ExecuteWhitelistRemove() {
            if (lstWlPlayers == null || lstWlPlayers.SelectedItem == null) {
                ShowWhitelistStatus("⚠️ Please select a player from the list to remove.", false);
                return;
            }
            string selectedPlayer = lstWlPlayers.SelectedItem.ToString();
            if (selectedPlayer.Equals("Primordial", StringComparison.OrdinalIgnoreCase)) {
                ShowWhitelistStatus("⚠️ Cannot remove host Primordial from whitelist.", false);
                return;
            }

            string password = GetHostPassword();
            ShowWhitelistStatus("⏳ Removing " + selectedPlayer + " from whitelist...", null);
            btnWlRemove.Enabled = false;

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    var payload = new Dictionary<string, object> {
                        { "host", "Primordial" },
                        { "password", password },
                        { "action", "remove" },
                        { "player", selectedPlayer }
                    };
                    string responseJson = SendWhitelistRequest(payload);
                    var serializer = new JavaScriptSerializer();
                    var resp = serializer.Deserialize<Dictionary<string, object>>(responseJson);

                    bool success = resp != null && resp.ContainsKey("success") && Convert.ToBoolean(resp["success"]);
                    string msg = resp != null && resp.ContainsKey("message") ? Convert.ToString(resp["message"]) : "Removed";

                    this.BeginInvoke((MethodInvoker)delegate {
                        btnWlRemove.Enabled = true;
                        if (success) {
                            ShowWhitelistStatus("✅ " + msg, true);
                            ExecuteWhitelistList();
                        } else {
                            ShowWhitelistStatus("❌ " + msg, false);
                        }
                    });
                } catch (Exception ex) {
                    this.BeginInvoke((MethodInvoker)delegate {
                        btnWlRemove.Enabled = true;
                        ShowWhitelistStatus("❌ Error: " + ex.Message, false);
                    });
                }
            });
        }

        private void ExecuteWhitelistList() {
            string password = GetHostPassword();
            if (btnWlRefresh != null) btnWlRefresh.Enabled = false;

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    var payload = new Dictionary<string, object> {
                        { "host", "Primordial" },
                        { "password", password },
                        { "action", "list" }
                    };
                    string responseJson = SendWhitelistRequest(payload);
                    var serializer = new JavaScriptSerializer();
                    var resp = serializer.Deserialize<Dictionary<string, object>>(responseJson);

                    bool success = resp != null && resp.ContainsKey("success") && Convert.ToBoolean(resp["success"]);
                    List<string> players = new List<string>();
                    if (success && resp.ContainsKey("whitelist")) {
                        var arr = resp["whitelist"] as System.Collections.ArrayList;
                        if (arr != null) {
                            foreach (object o in arr) {
                                if (o != null) players.Add(o.ToString());
                            }
                        }
                    }

                    this.BeginInvoke((MethodInvoker)delegate {
                        if (btnWlRefresh != null) btnWlRefresh.Enabled = true;
                        if (lstWlPlayers != null) {
                            lstWlPlayers.Items.Clear();
                            foreach (string p in players) {
                                lstWlPlayers.Items.Add(p);
                            }
                        }
                    });
                } catch (Exception) {
                    this.BeginInvoke((MethodInvoker)delegate {
                        if (btnWlRefresh != null) btnWlRefresh.Enabled = true;
                    });
                }
            });
        }

        private string SendWhitelistRequest(Dictionary<string, object> payload) {
            var serializer = new JavaScriptSerializer();
            string jsonBody = serializer.Serialize(payload);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(WhitelistApiUrl);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = bodyBytes.Length;
            request.Timeout = 12000;
            // Ignore certificate mismatches on self-hosted domains
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            using (Stream reqStream = request.GetRequestStream()) {
                reqStream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            try {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
                    return reader.ReadToEnd();
                }
            } catch (WebException wex) {
                if (wex.Response != null) {
                    using (StreamReader reader = new StreamReader(wex.Response.GetResponseStream(), Encoding.UTF8)) {
                        return reader.ReadToEnd();
                    }
                }
                throw;
            }
        }

        private void ShowWhitelistStatus(string message, bool? isSuccess) {
            if (lblWlStatus == null) return;
            lblWlStatus.Text = message;
            if (isSuccess == true) {
                lblWlStatus.ForeColor = Color.FromArgb(52, 211, 153); // Emerald
            } else if (isSuccess == false) {
                lblWlStatus.ForeColor = Color.FromArgb(239, 68, 68); // Red
            } else {
                lblWlStatus.ForeColor = Color.FromArgb(56, 189, 248); // Sky blue
            }
        }
    }
}
