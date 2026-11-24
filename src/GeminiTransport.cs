using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TranslateDesktop {
 public static class GeminiTransport {
  // Recreate the body for each attempt. Never retry key, quota or validation errors.
  public static async Task<HttpResponseMessage> PostAsync(HttpClient client,string url,string json,CancellationToken token,Action onAttempt=null,Func<TimeSpan,CancellationToken,Task> delay=null){
   for(int attempt=0;;attempt++){
    token.ThrowIfCancellationRequested();
    HttpResponseMessage response;
    using(var content=new StringContent(json,Encoding.UTF8,"application/json")){
     if(onAttempt!=null)onAttempt();
     response=await client.PostAsync(url,content,token);
    }
    int status=(int)response.StatusCode;
    if(attempt>=2||(status!=500&&status!=502&&status!=503&&status!=504))return response;
    TimeSpan pause=TimeSpan.FromMilliseconds(1000*(1<<attempt)+new Random().Next(0,251));
    var retry=response.Headers.RetryAfter;
    if(retry!=null){
     TimeSpan requested=retry.Delta??(retry.Date.HasValue?retry.Date.Value-DateTimeOffset.UtcNow:TimeSpan.Zero);
     // Long outages should return control to the user rather than block the editor.
     if(requested>TimeSpan.FromSeconds(10))return response;
     if(requested>pause)pause=requested;
    }
    response.Dispose();
    if(delay!=null)await delay(pause,token);else await Task.Delay(pause,token);
   }
  }
 }
}
