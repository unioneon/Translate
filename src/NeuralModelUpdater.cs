using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TranslateDesktop
{
 public static class OfflineUpdater
 {
  public static string ModelsRoot{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"offline-models");}}
  private static string ManifestPath{get{return Path.Combine(ModelsRoot,"active.json");}}
  public static string ActiveModelsPath
  {
   get{try{var manifest=Json.Parse(File.ReadAllText(ManifestPath));string folder=Json.String(manifest,"folder");if(!Regex.IsMatch(folder,@"^models-[a-f0-9]{32}$"))return null;string root=Path.Combine(ModelsRoot,folder);return ValidPair(root,"ru-en")&&ValidPair(root,"en-ru")?root:null;}catch{return null;}}
  }
  private static bool ValidPair(string root,string pair){return File.Exists(Path.Combine(root,pair,"metadata.json"))&&File.Exists(Path.Combine(root,pair,"sentencepiece.model"))&&File.Exists(Path.Combine(root,pair,"model","model.bin"));}
  public static async Task<string> DownloadAsync(Action<int> progress,CancellationToken token)
  {
   ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
   Directory.CreateDirectory(ModelsRoot);string staging=Path.Combine(ModelsRoot,"pending-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(staging);
   try
   {
    using(var client=new HttpClient{Timeout=TimeSpan.FromMinutes(20),MaxResponseContentBufferSize=4*1024*1024})
    {
     client.DefaultRequestHeaders.UserAgent.ParseAdd("Translate-Floating/4.0");progress(0);
     string index=await client.GetStringAsync("https://raw.githubusercontent.com/argosopentech/argospm-index/main/index.json");
     var entries=Json.Serializer.Deserialize<object[]>(index).Select(Json.Object).ToList();
     var selected=new List<Dictionary<string,object>>();
     foreach(string pair in new[]{"ru-en","en-ru"})
     {
      string[] codes=pair.Split('-');var model=entries.Where(item=>Json.String(item,"from_code")==codes[0]&&Json.String(item,"to_code")==codes[1]).OrderByDescending(item=>new Version(Json.String(item,"package_version"))).FirstOrDefault();
      if(model==null)throw new InvalidDataException("No language model");selected.Add(model);
     }
     string version=string.Join(" / ",selected.Select(item=>Json.String(item,"from_code")+"-"+Json.String(item,"to_code")+" "+Json.String(item,"package_version")));
     if(ActiveModelsPath!=null&&Json.String(Json.Parse(File.ReadAllText(ManifestPath)),"version")==version){progress(100);return version;}
     for(int i=0;i<selected.Count;i++)
     {
      token.ThrowIfCancellationRequested();var item=selected[i];var links=(IList)item["links"];
      string url=links.Cast<object>().Select(Convert.ToString).FirstOrDefault(link=>link.StartsWith("https://argos-net.com/",StringComparison.OrdinalIgnoreCase));
      if(url==null)throw new InvalidDataException("No official HTTPS model URL");
      string archive=Path.Combine(staging,"download.argosmodel");
      using(var response=await client.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,token))
      {
       response.EnsureSuccessStatusCode();long total=response.Content.Headers.ContentLength??0;if(total>800L*1024*1024)throw new InvalidDataException("Model is too large");
       using(var source=await response.Content.ReadAsStreamAsync())using(var file=File.Create(archive))
       {byte[] buffer=new byte[131072];long count=0;int read,last=-1;while((read=await source.ReadAsync(buffer,0,buffer.Length,token))>0){await file.WriteAsync(buffer,0,read,token);count+=read;if(count>800L*1024*1024)throw new InvalidDataException("Model is too large");int value=3+i*45+(total>0?(int)(42*count/total):0);if(value!=last){progress(value);last=value;}}}
      }
      string pair=Json.String(item,"from_code")+"-"+Json.String(item,"to_code");
      await Task.Run(()=>ExtractModel(archive,Path.Combine(staging,pair),item,token),token);File.Delete(archive);progress(47+i*45);
     }
     string installed="models-"+Guid.NewGuid().ToString("N");Directory.Move(staging,Path.Combine(ModelsRoot,installed));
     string temporary=ManifestPath+".tmp";File.WriteAllText(temporary,Json.Serializer.Serialize(new{folder=installed,version=version,source="https://github.com/argosopentech/argospm-index",engine="CTranslate2"}),new UTF8Encoding(false));
     if(File.Exists(ManifestPath))File.Replace(temporary,ManifestPath,null);else File.Move(temporary,ManifestPath);
     progress(100);return version;
    }
   }
   finally{if(Directory.Exists(staging)&&Path.GetFullPath(staging).StartsWith(Path.GetFullPath(ModelsRoot)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(staging,true);}
  }
  private static void ExtractModel(string archive,string destination,Dictionary<string,object> expected,CancellationToken token)
  {
   Directory.CreateDirectory(destination);long extracted=0;
   using(var zip=ZipFile.OpenRead(archive))
   {
    var metadata=zip.Entries.FirstOrDefault(entry=>entry.FullName.EndsWith("/metadata.json",StringComparison.Ordinal));if(metadata==null||metadata.Length>65536)throw new InvalidDataException("No model metadata");
    Dictionary<string,object> info;using(var stream=metadata.Open())using(var reader=new StreamReader(stream))info=Json.Parse(reader.ReadToEnd());
    if(Json.String(info,"from_code")!=Json.String(expected,"from_code")||Json.String(info,"to_code")!=Json.String(expected,"to_code"))throw new InvalidDataException("Wrong language model");
    string prefix=metadata.FullName.Substring(0,metadata.FullName.Length-"metadata.json".Length);
    foreach(var entry in zip.Entries)
    {
     token.ThrowIfCancellationRequested();if(!entry.FullName.StartsWith(prefix,StringComparison.Ordinal))continue;string relative=entry.FullName.Substring(prefix.Length);
     if(relative.EndsWith("/")||!(relative=="metadata.json"||relative=="sentencepiece.model"||relative=="README.md"||relative.StartsWith("model/",StringComparison.Ordinal)||relative.StartsWith("LICENSE",StringComparison.OrdinalIgnoreCase)))continue;
     string target=Path.GetFullPath(Path.Combine(destination,relative.Replace('/',Path.DirectorySeparatorChar)));
     if(!target.StartsWith(Path.GetFullPath(destination)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid archive path");
     extracted+=entry.Length;if(extracted>1200L*1024*1024)throw new InvalidDataException("Archive too large");Directory.CreateDirectory(Path.GetDirectoryName(target));entry.ExtractToFile(target,true);
    }
   }
   if(!File.Exists(Path.Combine(destination,"model","model.bin"))||!File.Exists(Path.Combine(destination,"sentencepiece.model")))throw new InvalidDataException("Incomplete model package");
  }
 }
}
