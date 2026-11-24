$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$framework='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$output=Join-Path $root '.cache\VerifyGeminiTransport.exe'
New-Item -ItemType Directory -Force -Path (Split-Path $output)|Out-Null
& (Join-Path $framework 'csc.exe') /nologo /target:exe ('/out:'+$output) ('/reference:'+(Join-Path $framework 'System.Net.Http.dll')) (Join-Path $root 'src\GeminiTransport.cs') (Join-Path $root 'tests\VerifyGeminiTransport.cs')
if($LASTEXITCODE-ne 0){throw 'Transport test compilation failed'}
& $output
if($LASTEXITCODE-ne 0){throw 'Transport verification failed'}
