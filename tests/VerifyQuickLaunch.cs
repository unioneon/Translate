using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;
using TranslateDesktop;
class VerifyQuickLaunch {
 [STAThread]static int Main(string[] args){try{return Run(args);}catch(Exception ex){File.WriteAllText(args[0],ex.ToString());return 1;}}
 static int Run(string[] args){
  var report=new List<string>();var app=new Application();var window=new MainWindow(false){Opacity=0,ShowActivated=false};int result=1;
  window.Loaded+=delegate{window.HideInTray();};
  window.Loaded+=async delegate{
   try{
    Check(QuickHotkey.Matches("Win+0",0x30,true,false,false,false),"default Win+0 chord",report);
    Check(!QuickHotkey.Matches("Win+0",0x30,true,true,false,false)&&!QuickHotkey.Matches("Win+0",0x30,true,false,false,true),"other Windows chords preserved",report);
    Check(QuickHotkey.Matches("Ctrl+Alt+T",0x54,false,true,true,false)&&!QuickHotkey.IsValid("Win+L"),"custom shortcut validation",report);
    var view=(WebView2CompositionControl)typeof(MainWindow).GetField("view",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(window);
    for(int n=0;n<100;n++){await Task.Delay(100);if((bool)typeof(MainWindow).GetField("ready",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(window))break;}
    Check(view.CoreWebView2!=null,"WebView initialized",report);
    Check(Math.Abs(window.Width-680)<1&&Math.Abs(view.ZoomFactor-1.0/1.5)<0.001,"window and interface reduced by 1.5",report);
    string viewport=await view.CoreWebView2.ExecuteScriptAsync("window.innerWidth");double logicalWidth;Check(double.TryParse(viewport,out logicalWidth)&&Math.Abs(logicalWidth-1020)<4,"original layout proportions preserved",report);
    window.ShowTranslator();await Task.Delay(150);
    await view.CoreWebView2.ExecuteScriptAsync("window.resizeTrace=[];window.originalPost=chrome.webview.postMessage.bind(chrome.webview);chrome.webview.postMessage=m=>{resizeTrace.push(m);originalPost(m);};document.querySelector('.to-provider-trigger').click()");await Task.Delay(350);
    string geometry=await view.CoreWebView2.ExecuteScriptAsync("JSON.stringify({viewport:innerHeight,menuBottom:document.querySelector('#to-provider-menu').getBoundingClientRect().bottom,menuHidden:document.querySelector('#to-provider-menu').hidden})");report.Add("Provider popup geometry: "+geometry+"; native height="+window.Height);
    Check(await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('#to-provider-menu').getBoundingClientRect().bottom <= innerHeight") == "true","provider menu fits viewport",report);
    await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('.to-provider-trigger').click();document.querySelector('.to-language-trigger').click()");await Task.Delay(150);
    Check(await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('#to-language-menu').getBoundingClientRect().bottom <= innerHeight") == "true","language menu fits viewport",report);
    await VerifyTranslationMenus(view,window,report);
    await view.CoreWebView2.ExecuteScriptAsync("document.querySelectorAll('.to-menu').forEach(m=>m.hidden=true)");
    await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('.to-language-trigger').click()");await Task.Delay(150);
    using(var screenshot=File.Create(Path.Combine(Path.GetDirectoryName(args[0]),"ui-verification","main-small.png")))await view.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,screenshot);
    window.Top=SystemParameters.WorkArea.Bottom-window.Height-5;
    await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('.to-provider-trigger').click()");await Task.Delay(150);Check(window.Top+window.Height<=SystemParameters.WorkArea.Bottom+2,"expanded menu stays on screen",report);
    await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('.to-input').value='Keep this text';document.querySelector('.to-input').dispatchEvent(new Event('input'))");await Task.Delay(150);
    await view.CoreWebView2.ExecuteScriptAsync("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true,cancelable:true}))");await Task.Delay(150);
    Check(!window.IsVisible,"Escape message hides native window",report);
    var timer=Stopwatch.StartNew();window.Left=12;window.Top=12;window.ShowTranslator();timer.Stop();await Task.Delay(250);
    Check(window.IsVisible,"warm show restores native window",report);CheckCentered(window,report);
    string text=await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('.to-input').value");Check(text.Contains("Keep this text"),"text preserved after hide and show",report);
    report.Add("Warm native show call: "+timer.ElapsedMilliseconds+" ms");
    await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('[data-i18n=settings]').click()");await Task.Delay(250);
    await view.CoreWebView2.ExecuteScriptAsync("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true,cancelable:true}))");await Task.Delay(150);Check(!window.IsVisible,"Escape hides settings window",report);
    window.ShowTranslator();await Task.Delay(250);CheckCentered(window,report);
    Check(await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('.app-settings').open") == "false","hotkey returns from settings to translator",report);result=0;
   }catch(Exception ex){report.Add("FAIL: "+ex.ToString());}
   finally{File.WriteAllLines(args[0],report);window.Close();}
  };
  var watchdog=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(25)};watchdog.Tick+=delegate{report.Add("FAIL timeout");File.WriteAllLines(args[0],report);window.Close();};watchdog.Start();app.Run(window);watchdog.Stop();return result;
 }
 static void Check(bool ok,string label,List<string> report){if(!ok)throw new Exception(label);report.Add("PASS "+label);}
 static async Task VerifyTranslationMenus(WebView2CompositionControl view,MainWindow window,List<string> report){
  view.CoreWebView2.PostWebMessageAsJson(Json.Serializer.Serialize(new{type="init",language="en",provider="Google Api",offlineReady=true,hasGoogleKey=false,systemLanguage="en",uiLanguage="en",hotkey="Win+0",winZeroEnabled=true}));await Task.Delay(100);
  await view.CoreWebView2.ExecuteScriptAsync("chrome.webview.postMessage=m=>{if(m.type==='translate'){window.pendingTranslation=m;return;}originalPost(m);};window.languageNode=document.querySelector('.to-languages button')");
  int selection=0;string[] languages={"ru","fr","es","de","uz","en","uk","kk","zh","ja","ko"};
  foreach(int lines in new[]{1,5,10,35,50,70,3,7,12,15,20}){
   await view.CoreWebView2.ExecuteScriptAsync("document.querySelectorAll('.to-menu').forEach(m=>m.hidden=true);document.querySelector('.to-input').value='Translate this sentence';document.querySelector('.to-input').dispatchEvent(new Event('input'));document.querySelector('form').dispatchEvent(new Event('submit',{cancelable:true,bubbles:true}));document.querySelector('.to-language-trigger').click()");await Task.Delay(150);
   Check(await view.CoreWebView2.ExecuteScriptAsync("!document.querySelector('#to-language-menu').hidden&&document.querySelector('.to-languages button').disabled") == "true","language menu opens safely during pending translation",report);
   int request=int.Parse(await view.CoreWebView2.ExecuteScriptAsync("pendingTranslation.requestId"));string translation="";for(int n=0;n<lines;n++)translation+="This is a complete translated sentence with punctuation.\n";
   view.CoreWebView2.PostWebMessageAsJson(Json.Serializer.Serialize(new{type="translation",requestId=request,ok=true,text=translation,source="ru",target="en"}));await Task.Delay(200);
   Check(window.IsVisible,"window stays visible when translation completes",report);
   Check(await view.CoreWebView2.ExecuteScriptAsync("!document.querySelector('#to-language-menu').hidden&&document.querySelector('#to-language-menu').getBoundingClientRect().bottom<=innerHeight+1&&document.querySelector('form').getBoundingClientRect().top>=0&&scrollY===0") == "true","open language menu fits after translation (lines="+lines+")",report);
   Check(await view.CoreWebView2.ExecuteScriptAsync("window.languageNode===document.querySelector('.to-languages button')&&!languageNode.disabled") == "true","language buttons retain identity and unlock",report);
   for(int attempt=0;attempt<3;attempt++){await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('.to-language-trigger').click()");await Task.Delay(30);await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('.to-language-trigger').click()");await Task.Delay(60);}
   Check(window.IsVisible&&await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('#to-language-menu').getBoundingClientRect().bottom<=innerHeight+1&&scrollY===0") == "true","repeated menu clicks do not clip or hide window",report);
   double height=window.Height,top=window.Top;string language=languages[selection++];
   await ClickElement(view,"[data-language='"+language+"']");await Task.Delay(150);
   Check(window.IsVisible&&Math.Abs(height-window.Height)<2&&Math.Abs(top-window.Top)<2,"selecting "+language+" after translation keeps native window stable",report);
   Check(await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('#to-language-menu').hidden&&!document.querySelector('#translator-original-draft').hidden&&document.querySelector('[data-language="+language+"]').getAttribute('aria-pressed')==='true'&&document.querySelector('.to-input').value.length>0&&scrollY===0") == "true","language selection preserves translated text and visible card",report);
   Check(await view.CoreWebView2.ExecuteScriptAsync("getComputedStyle(document.querySelector('[data-language="+language+"] .to-language-dot')).opacity==='1'&&!document.querySelector('.to-languages').textContent.includes('✓')") == "true","blue dot marks selected language without checkmark",report);
   await ClickElement(view,".to-language-trigger");await Task.Delay(100);Check(await view.CoreWebView2.ExecuteScriptAsync("document.querySelector('#to-language-menu').getBoundingClientRect().bottom<=innerHeight+1") == "true","menu reopens fully after changing language",report);
  }
 }
 static async Task ClickElement(WebView2CompositionControl view,string selector){string script="(()=>{const e=document.querySelector("+Json.Serializer.Serialize(selector)+");e.scrollIntoView({block:'nearest',inline:'nearest'});const r=e.getBoundingClientRect();return [r.left+r.width/2,r.top+r.height/2]})()";double[] point=Json.Serializer.Deserialize<double[]>(await view.CoreWebView2.ExecuteScriptAsync(script));await view.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",Json.Serializer.Serialize(new{type="mousePressed",x=point[0],y=point[1],button="left",clickCount=1}));await view.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",Json.Serializer.Serialize(new{type="mouseReleased",x=point[0],y=point[1],button="left",clickCount=1}));}
 static void CheckCentered(MainWindow window,List<string> report){var work=SystemParameters.WorkArea;double visual=(double)typeof(MainWindow).GetField("cardVisualHeight",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(window);double expected=Math.Max(work.Top,Math.Min(work.Top+(work.Height-visual)/2,work.Bottom-window.Height));Check(Math.Abs(window.Left-(work.Left+(work.Width-window.Width)/2))<5&&Math.Abs(window.Top-expected)<5,"visible card centered",report);}
}
