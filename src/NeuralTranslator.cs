using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TranslateDesktop
{
 public sealed class NeuralTranslator : IDisposable
 {
  private readonly string modelRoot;
  private readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
  private Process worker;
  private StreamWriter input;
  public NeuralTranslator(string models=null){modelRoot=models;}
  public async Task<string> Translate(string text,string source,string target,CancellationToken token)
  {
   if((source!="auto"&&source!="ru"&&source!="en")||(target!="ru"&&target!="en"))throw new InvalidOperationException("Offline поддерживает только русский ↔ английский.");
   if(source==target)return text;
   await gate.WaitAsync(token);
   try
   {
    string models=modelRoot??OfflineUpdater.ActiveModelsPath;
    if(models==null)throw new InvalidOperationException("Сначала скачайте модели Offline.");
    if(worker==null||worker.HasExited)
    {
     string runtime=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"offline-runtime");
     var info=new ProcessStartInfo(Path.Combine(runtime,"python.exe"),"-u \""+Path.Combine(runtime,"translate_worker.py")+"\" \""+models+"\"") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=runtime};
     info.EnvironmentVariables["PYTHONUTF8"]="1";
     worker=Process.Start(info);worker.ErrorDataReceived+=delegate{};worker.BeginErrorReadLine();
     input=new StreamWriter(worker.StandardInput.BaseStream,new UTF8Encoding(false)){AutoFlush=true};
    }
    await input.WriteLineAsync(Json.Serializer.Serialize(new{text=text,source=source,target=target}));
    Task<string> response=worker.StandardOutput.ReadLineAsync();
    if(await Task.WhenAny(response,Task.Delay(TimeSpan.FromSeconds(90),token))!=response){Stop();token.ThrowIfCancellationRequested();throw new InvalidOperationException("Офлайн-перевод занял слишком много времени. Попробуйте текст покороче.");}
    string line=await response;if(line==null){Stop();throw new InvalidOperationException("Не удалось запустить офлайн-движок. Проверьте файлы программы.");}
    var result=Json.Parse(line);
    if(!Json.String(result,"ok").Equals("True",StringComparison.OrdinalIgnoreCase))
    {string error=Json.String(result,"error");if(error=="PAIR_UNSUPPORTED")throw new InvalidOperationException("Offline поддерживает только русский ↔ английский.");throw new InvalidOperationException("Не удалось прочитать офлайн-модель. Скачайте модели заново.");}
    string translated=Json.String(result,"text");if(string.IsNullOrWhiteSpace(translated))throw new InvalidOperationException("Модель вернула пустой перевод.");return translated;
   }
   catch(System.ComponentModel.Win32Exception){throw new InvalidOperationException("Не удалось запустить офлайн-движок. Проверьте файлы программы.");}
   finally{gate.Release();}
  }
  private void Stop(){if(input!=null){try{input.Dispose();}catch{}input=null;}if(worker!=null){try{if(!worker.HasExited)worker.Kill();}catch{}worker.Dispose();worker=null;}}
  public void Dispose(){Stop();}
 }
}
