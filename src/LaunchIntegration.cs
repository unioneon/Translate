using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TranslateDesktop
{
 public static class HotkeyLauncher
 {
  private const string Marker = "Translate Floating keyboard launcher";
  private static string ShortcutPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Translate Floating.lnk"); } }
  public static bool IsValid(string value) { return value == "" || Regex.IsMatch(value, @"^Ctrl\+Alt\+[A-Z0-9]$"); }
  public static void Apply(string value)
  {
   if (!IsValid(value)) throw new InvalidOperationException("Нажмите Ctrl + Alt + букву или цифру.");
   string path = ShortcutPath;
   if (value.Length == 0 && !File.Exists(path)) return;
   object shell = null, link = null;
   try
   {
    shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
    link = shell.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
    if (File.Exists(path))
    {
     string description = Convert.ToString(link.GetType().InvokeMember("Description", BindingFlags.GetProperty, null, link, null));
     if (description != Marker) throw new InvalidOperationException("Имя ярлыка занято. Не удалось назначить горячую клавишу.");
    }
    if (value.Length == 0) { Marshal.FinalReleaseComObject(link); link = null; File.Delete(path); SHChangeNotify(0x4, 0x5, path, null); return; }
    ConfigureLink(link, value);
    link.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
    SHChangeNotify(0x2, 0x5, path, null);
   }
   finally { if (link != null) Marshal.FinalReleaseComObject(link); if (shell != null) Marshal.FinalReleaseComObject(shell); }
  }
  private static void ConfigureLink(object link, string value)
  {
   Action<string, object> set = (name, data) => link.GetType().InvokeMember(name, BindingFlags.SetProperty, null, link, new[] { data });
   set("TargetPath", Assembly.GetExecutingAssembly().Location); set("WorkingDirectory", AppDomain.CurrentDomain.BaseDirectory);
   set("Description", Marker); set("Hotkey", value); set("IconLocation", Assembly.GetExecutingAssembly().Location + ",0");
  }
  public static void TestShortcut(string path)
  {
   object shell = null, link = null;
   try
   {
    shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
    link = shell.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
    ConfigureLink(link, "Ctrl+Alt+T"); link.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
    string value = Convert.ToString(link.GetType().InvokeMember("Hotkey", BindingFlags.GetProperty, null, link, null));
    string[] parts=value.ToUpperInvariant().Split('+');
    if (parts.Length!=3||Array.IndexOf(parts,"CTRL")<0||Array.IndexOf(parts,"ALT")<0||Array.IndexOf(parts,"T")<0) throw new Exception("Shortcut hotkey roundtrip failed: "+value);
   }
   finally { if (link != null) Marshal.FinalReleaseComObject(link); if (shell != null) Marshal.FinalReleaseComObject(shell); if (File.Exists(path)) File.Delete(path); }
  }
  [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern void SHChangeNotify(uint change, uint flags, string item, string other);
 }
}
