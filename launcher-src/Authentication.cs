using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace PrimordialLauncher {
 public partial class LauncherForm {
  private TextBox txtPassword,txtConfirmPassword;
  private bool signingIn;
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
   forget.Click+=delegate {try{Credentials.Forget(txtNickname.Text.Trim());txtPassword.Clear();txtConfirmPassword.Clear();MessageBox.Show("Remembered password removed from this PC. For a server password reset, contact the host.");}catch{MessageBox.Show("Enter a valid nickname first.");}};tabPlay.Controls.Add(forget);
   txtNickname.Leave+=delegate{txtPassword.Text=Credentials.Load(txtNickname.Text.Trim());txtConfirmPassword.Clear();};
   txtPassword.Text=Credentials.Load(txtNickname.Text.Trim());
  }
  private void BeginAuthentication(string name) {
   if(signingIn)return;
   string mods=Path.Combine(GetInstanceFolderPath(),".minecraft","mods");
   if(!Directory.Exists(mods)||Directory.GetFiles(mods,"primordial-bridge-*.jar").Length==0){MessageBox.Show("Install the coordinated authentication pack before using this development launcher. The published launcher remains available for the existing server.");return;}
   string password=txtPassword.Text,confirmation=txtConfirmPassword.Text;
   string skin=activeSkin==null?"default":activeSkin.id;
   if(skin.StartsWith("custom_")||skin.StartsWith("cloned_")){MessageBox.Show("Select a bundled skin for multiplayer. Imported skins are previews only until custom synchronization is implemented.");return;}
   signingIn=true;btnPlayAction.Enabled=false;LogMessage("Checking approved account...");
   ThreadPool.QueueUserWorkItem(delegate {
    try {
     var status=AuthApi.Request("status",new {name=name});bool create=Convert.ToString(status["status"])=="unclaimed";
     if(create&&(password.Length<8||password.Length>128||password!=confirmation))throw new InvalidOperationException("To claim this approved nickname, enter a password of 8-128 characters and matching confirmation.");
     if(password.Length==0)throw new InvalidOperationException("Enter your account password.");
     var session=AuthApi.Request("session",new {name=name,password=password,register=create,skin=skin});
     Credentials.Save(name,password);string handoff=new JavaScriptSerializer().Serialize(session);
     if(IsDisposed||!IsHandleCreated)return;
     BeginInvoke(new Action(delegate {signingIn=false;sessionHandoff=handoff;LogMessage("Signed in securely. Password remembered on this PC.");LaunchGame(name);}));
    }catch(Exception error){if(!IsDisposed&&IsHandleCreated)BeginInvoke(new Action(delegate {signingIn=false;UpdateActionButton();LogMessage("Sign-in failed.");MessageBox.Show(error.Message,"Sign-in");}));}
   });
  }
  private void WriteSessionHandoff() {
   if(sessionHandoff==null)throw new InvalidOperationException("Sign in before launching.");
   string path=Path.Combine(GetInstanceFolderPath(),".minecraft","primordial-session.json");
   string expected=sessionHandoff;
   File.WriteAllText(path,expected);Credentials.ProtectFile(path);sessionHandoff=null;
   ThreadPool.QueueUserWorkItem(delegate {Thread.Sleep(125000);try{if(File.Exists(path)&&File.ReadAllText(path)==expected)File.Delete(path);}catch{}});
  }
 }
}
