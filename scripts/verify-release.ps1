param([string]$Version='1.1.0')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$zipPath=Join-Path $root ('dist\Translate-'+$Version+'-Portable-x64.zip')
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[IO.Compression.ZipFile]::OpenRead($zipPath)
try{
 $entries=$archive.Entries.FullName
 foreach($required in @('Translate/Translate.exe','Translate/Translate.Launcher.exe','Translate/offline-runtime/python.exe','Translate/offline-runtime/translate_worker.py','Translate/licenses/CTranslate2-LICENSE.txt','Translate/licenses/WebView2-LICENSE.txt')){if($entries-notcontains $required){throw ('Missing package file: '+$required)}}
 foreach($entry in $entries){if($entry-match '(?i)settings\.dat|WebView2Data|offline-models|__pycache__|\.pyc$|CaptureScreenshots|VerifyQuickLaunch|VerifySpelling|model\.bin|\.wav$'){throw ('Private or test artifact in ZIP: '+$entry)}}
 Write-Output ('PASS portable allowlist: '+$entries.Count+' entries')
}finally{$archive.Dispose()}
foreach($line in Get-Content (Join-Path $root 'dist\SHA256SUMS.txt')){if(!$line.Trim()){continue};$expected,$name=$line-split '  ',2;$file=Join-Path (Join-Path $root 'dist') $name;if((Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant()-ne $expected){throw ('Release checksum mismatch: '+$name)}}
Write-Output 'PASS release SHA-256 checksums'
$sourceFiles=Get-ChildItem -LiteralPath (Join-Path $root 'src') -File | Where-Object {$_.Extension-in '.cs','.js','.cjs','.css','.html','.ps1'}
foreach($file in $sourceFiles){$content=Get-Content -Raw -LiteralPath $file.FullName;if($content-match 'AIza[0-9A-Za-z_-]{30,}|AQ\.[0-9A-Za-z_-]{35,}|gh[pousr]_[0-9A-Za-z]{30,}'){throw ('Possible embedded credential: '+$file.Name)}}
Write-Output 'PASS source credential-pattern scan'
