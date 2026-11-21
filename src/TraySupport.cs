using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace TranslateDesktop
{
 public sealed class TraySupport : IDisposable
 {
  private readonly System.Windows.Forms.NotifyIcon tray;
  private readonly Action show, settings, exit;
  private string language;
  private string hotkey="Win+0";
  public TraySupport(Action onShow, Action onSettings, Action onExit, string uiLanguage)
  {
   show = onShow; settings = onSettings; exit = onExit; language = uiLanguage;
   tray = new System.Windows.Forms.NotifyIcon { Text = "Translate", Icon = CreateIcon(), Visible = true };
   tray.MouseUp += delegate(object sender, System.Windows.Forms.MouseEventArgs e) { if (e.Button == System.Windows.Forms.MouseButtons.Right) ShowMenu(); else if (e.Button == System.Windows.Forms.MouseButtons.Left) show(); };
  }
  public void SetLanguage(string value) { language = value; }
  public void SetHotkey(string value){hotkey=value;}
  private void ShowMenu()
  {
   bool ru = language == "ru";
   var menu = new ContextMenu { HasDropShadow = false, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), StaysOpen = false };
   var shell = new FrameworkElementFactory(typeof(Border));
   shell.SetValue(Border.BackgroundProperty, Brushes.White); shell.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(241,242,247))); shell.SetValue(Border.BorderThicknessProperty, new Thickness(1));
   shell.SetValue(Border.CornerRadiusProperty, new CornerRadius(18)); shell.SetValue(Border.PaddingProperty, new Thickness(7)); shell.SetValue(Border.MarginProperty, new Thickness(14));
   shell.SetValue(Border.EffectProperty, new DropShadowEffect { Color = Colors.Black, BlurRadius = 22, ShadowDepth = 3, Opacity = .14 });
   var items = new FrameworkElementFactory(typeof(ItemsPresenter)); shell.AppendChild(items);
   menu.Template = new ControlTemplate(typeof(ContextMenu)) { VisualTree = shell };
   Action<string,Action> add = (title, action) => {
    var item = new MenuItem { Header = title, FontFamily = new FontFamily("Segoe UI"), FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(100,103,119)), MinWidth = 220 };
    var border = new FrameworkElementFactory(typeof(Border), "ItemBorder"); border.SetValue(Border.CornerRadiusProperty, new CornerRadius(12)); border.SetValue(Border.PaddingProperty, new Thickness(14,10,14,10)); border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
    var text = new FrameworkElementFactory(typeof(ContentPresenter)); text.SetValue(ContentPresenter.ContentSourceProperty, "Header"); border.AppendChild(text);
    var template = new ControlTemplate(typeof(MenuItem)) { VisualTree = border };
    var hover = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true }; hover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(234,245,255)), "ItemBorder")); hover.Setters.Add(new Setter(MenuItem.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0,144,255)))); template.Triggers.Add(hover);
    item.Template = template; item.Click += delegate { menu.IsOpen = false; action(); }; menu.Items.Add(item);
   };
   add(hotkey==null?(ru?"Горячая клавиша отключена":"Launch shortcut is disabled"):(ru?"Для запуска нажмите ":"Press to launch: ")+hotkey,delegate{});((MenuItem)menu.Items[0]).IsEnabled=false;
   menu.Items.Add(new Separator{Margin=new Thickness(12,3,12,3),Background=new SolidColorBrush(Color.FromRgb(238,241,246))});
   add(ru ? "Открыть переводчик" : "Open translator", show); add(ru ? "Настройки" : "Settings", settings); add(ru ? "Выйти" : "Exit", exit);
   menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint; menu.IsOpen = true;
  }
  private static System.Drawing.Icon CreateIcon()
  {
   using (var bitmap = new System.Drawing.Bitmap(32,32))
   using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
   using (var blue = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0,144,255)))
   using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2.4f))
   {
    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; graphics.Clear(System.Drawing.Color.Transparent); graphics.FillEllipse(blue,1,1,30,30);
    pen.StartCap = pen.EndCap = System.Drawing.Drawing2D.LineCap.Round; graphics.DrawLine(pen,9,16,23,16); graphics.DrawLine(pen,17,10,23,16); graphics.DrawLine(pen,17,22,23,16);
    IntPtr handle = bitmap.GetHicon(); try { using (var icon = System.Drawing.Icon.FromHandle(handle)) return (System.Drawing.Icon)icon.Clone(); } finally { DestroyIcon(handle); }
   }
  }
  public void Dispose() { tray.Visible = false; var icon = tray.Icon; tray.Dispose(); icon.Dispose(); }
  [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
 }
}
