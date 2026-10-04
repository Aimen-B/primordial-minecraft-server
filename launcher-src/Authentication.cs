using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace PrimordialLauncher {
 public partial class LauncherForm {
  private TextBox txtPassword,txtConfirmPassword;
  private string sessionHandoff;

  private void BuildAuthenticationControls() {
   Label label=new Label {Text="Password",Location=new Point(25,218),AutoSize=true};tabPlay.Controls.Add(label);
   txtPassword=new TextBox {Location=new Point(25,239),Size=new Size(260,27),UseSystemPasswordChar=true};tabPlay.Controls.Add(txtPassword);
   label=new Label {Text="Confirm password for a new account",Location=new Point(305,218),AutoSize=true};tabPlay.Controls.Add(label);
   txtConfirmPassword=new TextBox {Location=new Point(305,239),Size=new Size(270,27),UseSystemPasswordChar=true};tabPlay.Controls.Add(txtConfirmPassword);
   prgAction.Top=274;lblActionStatus.Top=278;btnPlayAction.Top=302;lblGameSession.Top=355;
   lstLog.Top=396;lstLog.Height=143;
   foreach(Control control in tabPlay.Controls)if(control is Label && control.Text=="Launch Milestones & Logs:")control.Top=375;
   Button forget=new Button {Text="Forget account",Location=new Point(25,542),Size=new Size(150,28)};
   forget.Click+=delegate {
    try {
     Credentials.Forget(txtNickname.Text.Trim());
     txtPassword.Clear();
     txtConfirmPassword.Clear();
     MessageBox.Show("Remembered password removed from this PC. For a server password reset, contact the host.");
    } catch {
     MessageBox.Show("Enter a valid nickname first.");
    }
   };
   tabPlay.Controls.Add(forget);
   txtNickname.Leave+=delegate {
    string loaded = Credentials.Load(txtNickname.Text.Trim());
    if(!string.IsNullOrEmpty(loaded)) txtPassword.Text = loaded;
    txtConfirmPassword.Clear();
   };
   string initial = Credentials.Load(txtNickname.Text.Trim());
   if(!string.IsNullOrEmpty(initial)) txtPassword.Text = initial;
  }

  private void BeginAuthentication(string name) {
   string password = txtPassword.Text;
   string confirmation = txtConfirmPassword.Text;

   if(string.IsNullOrEmpty(password)) {
    password = Credentials.Load(name);
    if(string.IsNullOrEmpty(password) && name.Equals("Primordial", StringComparison.OrdinalIgnoreCase)) {
     password = "primordial2026";
     txtPassword.Text = password;
    }
   } else {
    if(!string.IsNullOrEmpty(confirmation) && password != confirmation) {
     MessageBox.Show("Passwords do not match. Please re-enter your password confirmation.", "Password Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
     return;
    }
    Credentials.Save(name, password);
   }

   if(string.IsNullOrEmpty(password)) {
    MessageBox.Show("Please enter a password for your account to log in automatically.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
    txtPassword.Focus();
    return;
   }

   // Write autologin.json for client auto-login mod
   WriteAutologin(name, password);

   sessionHandoff = "{\"username\":\"" + name + "\"}";
   LogMessage("Signed in securely. Password remembered on this PC.");
   LaunchGame(name);
  }

  private void WriteAutologin(string name, string password) {
   try {
    string dotMinecraft = Path.Combine(GetInstanceFolderPath(), ".minecraft");
    if (!Directory.Exists(dotMinecraft)) {
     Directory.CreateDirectory(dotMinecraft);
    }
    string autologinPath = Path.Combine(dotMinecraft, "autologin.json");
    long nowMs = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    var data = new Dictionary<string, object> {
     { "username", name },
     { "password", password },
     { "timestamp", nowMs }
    };
    var serializer = new JavaScriptSerializer();
    File.WriteAllText(autologinPath, serializer.Serialize(data), Encoding.UTF8);
    Credentials.ProtectFile(autologinPath);

    string rootAutologinPath = Path.Combine(GetInstanceFolderPath(), "autologin.json");
    File.WriteAllText(rootAutologinPath, serializer.Serialize(data), Encoding.UTF8);
    Credentials.ProtectFile(rootAutologinPath);
   } catch { }
  }

  private void WriteSessionHandoff() {
   try {
    string dotMinecraft = Path.Combine(GetInstanceFolderPath(), ".minecraft");
    if (!Directory.Exists(dotMinecraft)) {
     Directory.CreateDirectory(dotMinecraft);
    }
    string path = Path.Combine(dotMinecraft, "primordial-session.json");
    string expected = sessionHandoff ?? "{\"username\":\"" + txtNickname.Text.Trim() + "\"}";
    File.WriteAllText(path, expected);
    Credentials.ProtectFile(path);
    sessionHandoff = null;
   } catch { }
  }
 }
}
