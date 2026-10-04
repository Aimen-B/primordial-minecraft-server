using System;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace PrimordialLauncher {
 public static class Credentials {
  private static readonly byte[] Entropy=Encoding.UTF8.GetBytes("Primordial Adventures credentials v1");
  private static string PathFor(string name) {
   if(!System.Text.RegularExpressions.Regex.IsMatch(name,@"^[A-Za-z0-9_]{3,16}$"))throw new ArgumentException("Invalid nickname.");
   string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PrimordialAdventuresCredentials");
   Directory.CreateDirectory(dir);return Path.Combine(dir,name+".bin");
  }
  public static void ProtectFile(string path) {
   FileSecurity acl=new FileSecurity();acl.SetAccessRuleProtection(true,false);
   acl.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User,FileSystemRights.FullControl,AccessControlType.Allow));
   acl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid,null),FileSystemRights.FullControl,AccessControlType.Allow));
   File.SetAccessControl(path,acl);
  }
  public static void Save(string name,string password) {
   string path=PathFor(name);byte[] plain=Encoding.UTF8.GetBytes(password);
   try { File.WriteAllBytes(path,ProtectedData.Protect(plain,Entropy,DataProtectionScope.CurrentUser));ProtectFile(path); }
   finally { Array.Clear(plain,0,plain.Length); }
  }
  public static string Load(string name) {
   try { byte[] plain=ProtectedData.Unprotect(File.ReadAllBytes(PathFor(name)),Entropy,DataProtectionScope.CurrentUser);
    try{return Encoding.UTF8.GetString(plain);}finally{Array.Clear(plain,0,plain.Length);} }
   catch{return "";}
  }
  public static void Forget(string name) { string path=PathFor(name);if(File.Exists(path))File.Delete(path); }
 }
 public static class AuthApi {
  public static Dictionary<string,object> Request(string route,object input) {
   ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
   var json=new JavaScriptSerializer();
   var request=(HttpWebRequest)WebRequest.Create("https://minecraft.primordial.my/api/auth/"+route);
   request.Method="POST";request.AllowAutoRedirect=false;request.Timeout=15000;request.ReadWriteTimeout=15000;
   request.ContentType="application/json";byte[] body=Encoding.UTF8.GetBytes(json.Serialize(input));request.ContentLength=body.Length;
   try {
    using(var stream=request.GetRequestStream())stream.Write(body,0,body.Length);
    HttpWebResponse response;
    try{response=(HttpWebResponse)request.GetResponse();}
    catch(WebException error){response=error.Response as HttpWebResponse;if(response==null)throw new InvalidOperationException("Authentication server unavailable. Try again later.");}
    using(response)using(var reader=new StreamReader(response.GetResponseStream())) {
     Dictionary<string,object> output;
     try{output=json.Deserialize<Dictionary<string,object>>(reader.ReadToEnd());}catch{throw new InvalidOperationException("The server authentication update is not available yet.");}
     string status=Convert.ToString(output["status"]);
     if(status!="ok" && status!="claimed" && status!="unclaimed")throw new InvalidOperationException(Friendly(status));
     return output;
    }
   } finally {Array.Clear(body,0,body.Length);}
  }
  private static string Friendly(string status) {
   switch(status) {
    case "not_whitelisted":return "Ask the host to whitelist your exact nickname.";
    case "wrong_password":return "Incorrect password. Contact the host if you need a reset.";
    case "already_claimed":return "This nickname is already claimed. Sign in with its existing password.";
    case "invalid_password":return "New passwords need 8-128 characters.";
    case "try_later":return "Too many attempts. Wait one minute and try again.";
    default:return "Authentication unavailable. Try again later or contact the host.";
   }
  }
 }
}
