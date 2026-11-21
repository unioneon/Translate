using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TranslateDesktop;

namespace TranslateQuickLaunch {
 static class Program {
  const string Marker="Translate Win+0 launcher";
  static readonly string Identity=System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
  static readonly string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TranslateFloating");
  static readonly string Shortcut=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"Translate Win+0.lnk");
  static HookProc callback=Keyboard;static IntPtr hook;static uint capturedKey;static long lastActivation;static int launching;static string hotkey="Win+0";
  static void ReloadHotkey(){try{string value=File.ReadAllText(Path.Combine(Root,"launcher-hotkey.txt")).Trim();if(QuickHotkey.IsValid(value))hotkey=value;}catch{}}
  delegate IntPtr HookProc(int code,IntPtr message,IntPtr data);
  [StructLayout(LayoutKind.Sequential)]struct KeyData{public uint vk,scan,flags,time;public UIntPtr extra;}
  [DllImport("user32.dll",SetLastError=true)]static extern IntPtr SetWindowsHookEx(int type,HookProc proc,IntPtr module,uint thread);
  [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr value);
  [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr value,int code,IntPtr message,IntPtr data);
  [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
  [DllImport("user32.dll")]static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
  [DllImport("user32.dll")]static extern bool AllowSetForegroundWindow(int pid);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string name);
  static IntPtr Keyboard(int code,IntPtr message,IntPtr data){
   if(code>=0){var key=(KeyData)Marshal.PtrToStructure(data,typeof(KeyData));int msg=message.ToInt32();bool down=msg==0x100||msg==0x104,up=msg==0x101||msg==0x105;
    if((key.flags&0x10)==0){
     if(up&&key.vk==capturedKey){capturedKey=0;return new IntPtr(1);}
     bool win=GetAsyncKeyState(0x5B)<0||GetAsyncKeyState(0x5C)<0;
     if(down&&(key.vk==capturedKey||QuickHotkey.Matches(hotkey,key.vk,win,GetAsyncKeyState(0x11)<0,GetAsyncKeyState(0x12)<0,GetAsyncKeyState(0x10)<0))){
      if(capturedKey==0){capturedKey=key.vk;if(win){keybd_event(0xE8,0,0,UIntPtr.Zero);keybd_event(0xE8,0,2,UIntPtr.Zero);}Task.Run((Action)Activate);}
      return new IntPtr(1);
     }
    }
   }
   return CallNextHookEx(hook,code,message,data);
  }
  static void Activate(){
   if(Interlocked.Exchange(ref launching,1)!=0)return;
   try{
    long now=Environment.TickCount&int.MaxValue;if(now-lastActivation<350&&lastActivation!=0)return;lastActivation=now;
    string path=File.ReadAllText(Path.Combine(Root,"launcher-target.txt")).Trim();
    if(!Path.IsPathRooted(path)||!File.Exists(path)||!string.Equals(Path.GetFileName(path),"Translate.exe",StringComparison.OrdinalIgnoreCase))return;
    try{using(var signal=EventWaitHandle.OpenExisting("Local\\TranslateFloating.Show."+Identity)){
     try{int pid; if(int.TryParse(File.ReadAllText(Path.Combine(Root,"app-pid.txt")),out pid))using(var process=Process.GetProcessById(pid)){if(string.Equals(process.MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))AllowSetForegroundWindow(pid);}}catch{}
     signal.Set();return;
    }}catch(WaitHandleCannotBeOpenedException){}
    Process.Start(new ProcessStartInfo(path){WorkingDirectory=Path.GetDirectoryName(path),UseShellExecute=false});
   }catch{}finally{Interlocked.Exchange(ref launching,0);}
  }
  static void Configure(bool enable){
   object shell=null,link=null;
   try{
    shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));link=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{Shortcut});
    if(File.Exists(Shortcut)&&Convert.ToString(link.GetType().InvokeMember("Description",BindingFlags.GetProperty,null,link,null))!=Marker)throw new InvalidOperationException("Startup shortcut is owned by another app");
    if(!enable){if(File.Exists(Shortcut))File.Delete(Shortcut);return;}
    Action<string,object> set=(name,value)=>link.GetType().InvokeMember(name,BindingFlags.SetProperty,null,link,new[]{value});
    set("TargetPath",Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Translate.exe"));set("Arguments","--background");set("WorkingDirectory",AppDomain.CurrentDomain.BaseDirectory);set("Description",Marker);set("WindowStyle",7);link.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);
   }finally{if(link!=null)Marshal.FinalReleaseComObject(link);if(shell!=null)Marshal.FinalReleaseComObject(shell);}
  }
  [STAThread]static int Main(string[] args){
   try{
    bool enable=args.Length>0&&args[0]=="--enable",disable=args.Length>0&&args[0]=="--disable";
    if(args.Length>0&&args[0]=="--uninstall"){
     string target=Path.Combine(Root,"launcher-target.txt");
     if(!File.Exists(target)||!string.Equals(File.ReadAllText(target).Trim(),Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Translate.exe"),StringComparison.OrdinalIgnoreCase))return 0;
     disable=true;
    }
    if(enable){Directory.CreateDirectory(Root);File.WriteAllText(Path.Combine(Root,"launcher-target.txt"),Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Translate.exe"));Configure(true);}
    if(disable){Configure(false);try{using(var stop=EventWaitHandle.OpenExisting("Local\\TranslateFloating.LauncherStop."+Identity))stop.Set();}catch(WaitHandleCannotBeOpenedException){}return 0;}
    if(args.Length>0&&args[0]=="--activate"){Activate();return 0;}
    bool created;using(var mutex=new Mutex(true,"Local\\TranslateFloating.Launcher."+Identity,out created)){
     if(!created)return 0;
     using(var stop=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\TranslateFloating.LauncherStop."+Identity))
     using(var context=new ApplicationContext()){
      ReloadHotkey();hook=SetWindowsHookEx(13,callback,GetModuleHandle(null),0);if(hook==IntPtr.Zero)return 2;
      var timer=new System.Windows.Forms.Timer{Interval=400};timer.Tick+=delegate{ReloadHotkey();if(stop.WaitOne(0))context.ExitThread();};timer.Start();
      try{Application.Run(context);}finally{timer.Dispose();UnhookWindowsHookEx(hook);}
     }mutex.ReleaseMutex();
    }return 0;
   }catch{return 1;}
  }
 }
}
