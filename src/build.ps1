param([string]$OutputDirectory = 'release-v2')
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
if([IO.Path]::IsPathRooted($OutputDirectory)){$OutputDirectory=[IO.Path]::GetRelativePath($PSScriptRoot,$OutputDirectory)}
node .\prepare-ui.cjs
if ($LASTEXITCODE -ne 0) { throw 'HTML preparation failed' }
$framework='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$libraries=Join-Path $PSScriptRoot 'packages\webview2-new\lib\net462'
$references=@('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Net.Http.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Web.Extensions.dll','System.Security.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','WPF\System.Speech.dll')
New-Item -ItemType Directory -Force -Path (Join-Path $PSScriptRoot $OutputDirectory) | Out-Null
$arguments=@('/nologo','/target:winexe','/platform:x64','/optimize+',('/out:'+(Join-Path $OutputDirectory 'Translate.exe')),'/resource:index.html,index.html','/win32manifest:app.manifest')
foreach($reference in $references) { $arguments += '/reference:'+(Join-Path $framework $reference) }
foreach($reference in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.Wpf.dll')) { $arguments += '/reference:'+(Join-Path $libraries $reference); $destinationLibrary=Join-Path (Join-Path $PSScriptRoot $OutputDirectory) $reference; if (!(Test-Path -LiteralPath $destinationLibrary)) { Copy-Item -LiteralPath (Join-Path $libraries $reference) -Destination $destinationLibrary } }
$arguments += @('DesktopApp.cs','LaunchIntegration.cs','TraySupport.cs','NeuralModelUpdater.cs','NeuralTranslator.cs','VoiceInput.cs','QuickLaunchIntegration.cs','QuickHotkey.cs','GeminiSpelling.cs','GeminiTransport.cs','AssemblyInfo.cs')
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'assets\app.ico')){$arguments+='/win32icon:'+(Join-Path $PSScriptRoot 'assets\app.ico')}
& (Join-Path $framework 'csc.exe') @arguments
if($LASTEXITCODE -ne 0) { throw 'Build failed' }
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 /optimize+ ('/out:'+(Join-Path $OutputDirectory 'Translate.Launcher.exe')) /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll QuickLauncher.cs QuickHotkey.cs
if($LASTEXITCODE -ne 0){throw 'Quick launcher build failed'}
if (!(Test-Path -LiteralPath (Join-Path $PSScriptRoot (Join-Path $OutputDirectory 'WebView2Loader.dll')))) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'packages\webview2-new\runtimes\win-x64\native\WebView2Loader.dll') -Destination (Join-Path $PSScriptRoot $OutputDirectory) }
$runtimeDestination=Join-Path (Join-Path $PSScriptRoot $OutputDirectory) 'offline-runtime'
foreach($runtimeFile in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'neural-runtime') -Recurse -File) {
 if($runtimeFile.FullName -match '__pycache__|\.pyc$'){continue}
 $relativeRuntimePath=[IO.Path]::GetRelativePath((Join-Path $PSScriptRoot 'neural-runtime'),$runtimeFile.FullName)
 $destinationRuntimeFile=Join-Path $runtimeDestination $relativeRuntimePath
 New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($destinationRuntimeFile)) | Out-Null
 if(!(Test-Path -LiteralPath $destinationRuntimeFile) -or $runtimeFile.Name -eq 'translate_worker.py'){Copy-Item -LiteralPath $runtimeFile.FullName -Destination $destinationRuntimeFile -Force}
}
Write-Output 'Built tray-only Translate.exe with offline neural sentence translation.'
