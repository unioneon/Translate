using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;
using TranslateDesktop;
class CaptureScreenshots {
 [STAThread]static int Main(string[] args){var app=new Application();var window=new MainWindow(false){Opacity=0,ShowActivated=false};int code=1;window.Loaded+=async delegate{
  try{
   var view=(WebView2CompositionControl)typeof(MainWindow).GetField("view",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(window);
   for(int n=0;n<100;n++){await Task.Delay(100);if((bool)typeof(MainWindow).GetField("ready",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(window))break;}
   window.ShowTranslator();await Task.Delay(150);Directory.CreateDirectory(args[0]);
   string translated;using(var service=new Translator(args[1]))translated=await service.Translate("Доброе утро! Как твои дела?","ru","en","Offline",new Settings(),CancellationToken.None);
   view.CoreWebView2.PostWebMessageAsJson(Json.Serializer.Serialize(new{type="init",language="en",provider="Offline",offlineReady=true,hasGoogleKey=false,systemLanguage="en",uiLanguage="en",hotkey="Win+0",winZeroEnabled=true}));await Task.Delay(80);
   view.CoreWebView2.PostWebMessageAsJson(Json.Serializer.Serialize(new{type="translation",requestId=0,ok=true,text=translated,source="ru",target="en"}));await Task.Delay(150);await Capture(view,Path.Combine(args[0],"translation.png"),false);
   await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('.to-language-trigger').click()");await Task.Delay(100);await Capture(view,Path.Combine(args[0],"languages.png"),true);
   await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('.to-language-trigger').click();document.querySelector('textarea').value='';document.querySelector('textarea').dispatchEvent(new Event('input'))");
   view.CoreWebView2.PostWebMessageAsJson(Json.Serializer.Serialize(new{type="init",language="en",provider="Offline",offlineReady=false,hasGoogleKey=false,systemLanguage="en",uiLanguage="en",hotkey="Win+0",winZeroEnabled=true}));await Task.Delay(120);await Capture(view,Path.Combine(args[0],"offline.png"),false);
   view.CoreWebView2.PostWebMessageAsJson(Json.Serializer.Serialize(new{type="openSettings"}));await Task.Delay(150);await Capture(view,Path.Combine(args[0],"settings.png"),false);
   File.WriteAllText(Path.Combine(args[0],"capture-notes.txt"),"Screenshots captured from the compiled Translate app. Demo translation was produced by the bundled offline engine: "+translated+". No private keys, user files or recordings were used.");code=0;
  }catch(Exception ex){File.WriteAllText(Path.Combine(args[0],"capture-error.txt"),ex.ToString());}
  finally{window.Close();}
 };app.Run(window);return code;}
 static async Task Capture(WebView2CompositionControl view,string path,bool menu){
  string dimensions=await view.CoreWebView2.ExecuteScriptAsync("(()=>{if(document.querySelector('.app-settings').open){const r=document.querySelector('.app-settings').getBoundingClientRect();return [Math.max(0,r.left-25),0,r.width+50,Math.ceil(r.bottom+25)];}let bottom=document.querySelector('form').getBoundingClientRect().bottom;document.querySelectorAll('.to-menu:not([hidden])').forEach(m=>bottom=Math.max(bottom,m.getBoundingClientRect().bottom));return [0,0,innerWidth,Math.ceil(bottom+25)];})()");
  double[] size=Json.Serializer.Deserialize<double[]>(dimensions);
  double zoom=view.ZoomFactor;string result=await view.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot",Json.Serializer.Serialize(new{format="png",captureBeyondViewport=false,clip=new{x=size[0]*zoom,y=size[1]*zoom,width=size[2]*zoom,height=size[3]*zoom,scale=1}}));
  File.WriteAllBytes(path,Convert.FromBase64String(Json.String(Json.Parse(result),"data")));
 }
}
