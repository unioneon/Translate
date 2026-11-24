param([string]$Version='1.1.1',[string]$AppDirectory='')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
if(!$AppDirectory){$AppDirectory=Join-Path $root ('dist\Translate-'+$Version+'-Windows-x64')}
$AppDirectory=[IO.Path]::GetFullPath($AppDirectory)
$framework='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$executable=Join-Path $AppDirectory 'VerifySpelling.exe'
$report=Join-Path $root '.cache\verification-spelling.txt'
& (Join-Path $framework 'csc.exe') /nologo /platform:x64 /target:winexe ('/out:'+$executable) ('/reference:'+(Join-Path $AppDirectory 'Translate.exe')) ('/reference:'+(Join-Path $AppDirectory 'Microsoft.Web.WebView2.Core.dll')) ('/reference:'+(Join-Path $AppDirectory 'Microsoft.Web.WebView2.Wpf.dll')) ('/reference:'+(Join-Path $framework 'WPF\WindowsBase.dll')) ('/reference:'+(Join-Path $framework 'WPF\PresentationCore.dll')) ('/reference:'+(Join-Path $framework 'WPF\PresentationFramework.dll')) ('/reference:'+(Join-Path $framework 'System.Xaml.dll')) (Join-Path $root 'tests\VerifySpelling.cs')
if($LASTEXITCODE-ne 0){throw 'Spelling test compilation failed'}
try{
 $probe=Start-Process -FilePath $executable -ArgumentList $report -WindowStyle Hidden -PassThru
 if(!$probe.WaitForExit(40000)){Stop-Process -Id $probe.Id;throw 'Spelling test timed out'}
 Get-Content -LiteralPath $report
 if($probe.ExitCode-ne 0-or (Select-String -LiteralPath $report -Pattern '^FAIL' -Quiet)){throw 'Spelling verification failed'}
}finally{Remove-Item -LiteralPath $executable -Force}
