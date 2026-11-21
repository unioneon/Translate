using System;
using System.Diagnostics;
using System.IO;
namespace TranslateDesktop {
 public static class QuickLaunchIntegration {
  public static void Apply(bool enabled,string hotkey){
   if(!QuickHotkey.IsValid(hotkey))throw new InvalidOperationException("Выберите Win + букву или цифру (кроме Win+L), либо Ctrl + Alt + букву или цифру.");
   string helper=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Translate.Launcher.exe");
   if(!File.Exists(helper))throw new InvalidOperationException("Не найден Translate.Launcher.exe. Сохраните все файлы программы в одной папке.");
   string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TranslateFloating");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"launcher-hotkey.txt"),hotkey);
   Process.Start(new ProcessStartInfo(helper,enabled?"--enable":"--disable"){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory});
  }
  public static void SaveProcessId(){try{string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TranslateFloating");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"app-pid.txt"),Process.GetCurrentProcess().Id.ToString());}catch{}}
 }
}
