using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using TranslateDesktop;

class VerifyGeminiTransport {
 sealed class Handler:HttpMessageHandler {
  public int Calls;public int[] Statuses;public int RetrySeconds;public bool Header=true;public List<string> Bodies=new List<string>();
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
   token.ThrowIfCancellationRequested();Header&=request.Headers.Contains("x-goog-api-key");Bodies.Add(await request.Content.ReadAsStringAsync());
   var response=new HttpResponseMessage((HttpStatusCode)Statuses[Math.Min(Calls++,Statuses.Length-1)]){Content=new StringContent("{\"result\":\"ok\"}")};
   if(RetrySeconds>0)response.Headers.RetryAfter=new RetryConditionHeaderValue(TimeSpan.FromSeconds(RetrySeconds));return response;
  }
 }
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static int Main(){try{Run().GetAwaiter().GetResult();return 0;}catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}}
 static async Task Run(){
  var waits=new List<TimeSpan>();Func<TimeSpan,CancellationToken,Task> delay=(pause,token)=>{token.ThrowIfCancellationRequested();waits.Add(pause);return Task.FromResult(0);};
  foreach(int status in new[]{500,502,503,504}){
   var handler=new Handler{Statuses=new[]{status,200}};using(var client=new HttpClient(handler)){
    client.DefaultRequestHeaders.Add("x-goog-api-key","test-placeholder");
    using(var response=await GeminiTransport.PostAsync(client,"https://example.invalid/test","Привет",CancellationToken.None,null,delay))Check(response.IsSuccessStatusCode&&handler.Calls==2&&handler.Bodies.TrueForAll(body=>body=="Привет")&&handler.Header,"transient "+status+" retries with intact UTF-8 body and key header");
   }
  }
  var unavailable=new Handler{Statuses=new[]{503}};waits.Clear();using(var client=new HttpClient(unavailable))using(var response=await GeminiTransport.PostAsync(client,"https://example.invalid/test","{}",CancellationToken.None,null,delay)){
   Check((int)response.StatusCode==503&&unavailable.Calls==3,"persistent 503 stops after three attempts");
   Check(waits.Count==2&&waits[0].TotalMilliseconds>=1000&&waits[0].TotalMilliseconds<=1250&&waits[1].TotalMilliseconds>=2000&&waits[1].TotalMilliseconds<=2250,"bounded exponential pauses with jitter");
  }
  foreach(int status in new[]{200,400,401,403,404,429}){
   var handler=new Handler{Statuses=new[]{status}};using(var client=new HttpClient(handler))using(var response=await GeminiTransport.PostAsync(client,"https://example.invalid/test","{}",CancellationToken.None,null,delay))Check(handler.Calls==1&&(int)response.StatusCode==status,"no retry for "+status);
  }
  var busy=new Handler{Statuses=new[]{503,200},RetrySeconds=5};waits.Clear();using(var client=new HttpClient(busy))using(var response=await GeminiTransport.PostAsync(client,"https://example.invalid/test","{}",CancellationToken.None,null,delay))Check(waits.Count==1&&waits[0]==TimeSpan.FromSeconds(5),"Retry-After respected");
  var outage=new Handler{Statuses=new[]{503},RetrySeconds=120};using(var client=new HttpClient(outage))using(var response=await GeminiTransport.PostAsync(client,"https://example.invalid/test","{}",CancellationToken.None,null,delay))Check(outage.Calls==1,"long Retry-After returns control without early retry");
  using(var cancellation=new CancellationTokenSource()){
   var handler=new Handler{Statuses=new[]{503}};using(var client=new HttpClient(handler)){
    bool cancelled=false;try{await GeminiTransport.PostAsync(client,"https://example.invalid/test","{}",cancellation.Token,null,(pause,token)=>{cancellation.Cancel();token.ThrowIfCancellationRequested();return Task.FromResult(0);});}catch(OperationCanceledException){cancelled=true;}
    Check(cancelled&&handler.Calls==1,"cancellation during backoff prevents further requests");
   }
  }
 }
}
