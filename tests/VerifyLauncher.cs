using System;
using System.IO;
using System.Reflection;
using System.Threading;
class VerifyLauncher {
 static int Main(string[] args){try{
  string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);var assembly=Assembly.LoadFrom(Path.GetFullPath(args[1]));var type=assembly.GetType("TranslateQuickLaunch.Program");const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Static;
  Action<string,object> set=(name,value)=>type.GetField(name,flags).SetValue(null,value);
  Func<string,object[],object> call=(name,values)=>type.GetMethod(name,flags).Invoke(null,values);
  string identity="Test-"+Guid.NewGuid().ToString("N");set("Root",root);set("Identity",identity);set("Shortcut",Path.Combine(root,"Startup-test.lnk"));
  call("Configure",new object[]{true});if(!File.Exists(Path.Combine(root,"Startup-test.lnk")))throw new Exception("Startup shortcut was not created");
  object shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));object link=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{Path.Combine(root,"Startup-test.lnk")});string arguments=Convert.ToString(link.GetType().InvokeMember("Arguments",BindingFlags.GetProperty,null,link,null));if(arguments!="--background")throw new Exception("Incorrect startup arguments");System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);call("Configure",new object[]{false});Console.WriteLine("PASS background startup shortcut, tested in workspace only");
  File.WriteAllText(Path.Combine(root,"launcher-target.txt"),Path.Combine(root,"Translate.exe"));call("Activate",new object[0]);for(int i=0;i<60&&!File.Exists(Path.Combine(root,"cold.txt"));i++)Thread.Sleep(100);if(!File.Exists(Path.Combine(root,"cold.txt")))throw new Exception("Cold launch failed");Console.WriteLine("PASS starts an executable when the application is closed");
  using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\TranslateFloating.Show."+identity)){set("lastActivation",(long)0);call("Activate",new object[0]);if(!signal.WaitOne(2000))throw new Exception("Warm signal missing");Console.WriteLine("PASS signals existing application without starting another process");}
  object callback=type.GetField("callback",flags).GetValue(null);IntPtr module=(IntPtr)call("GetModuleHandle",new object[]{null});IntPtr hook=(IntPtr)call("SetWindowsHookEx",new object[]{13,callback,module,(uint)0});if(hook==IntPtr.Zero)throw new Exception("Keyboard hook registration failed");call("UnhookWindowsHookEx",new object[]{hook});Console.WriteLine("PASS keyboard hook registers and releases");return 0;
 }catch(Exception ex){Console.WriteLine("FAIL: "+ex);return 1;}}
}
