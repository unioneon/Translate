param([switch]$Installer,[switch]$SkipBootstrap,[string]$Version='1.1.1')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
if(!$SkipBootstrap){& (Join-Path $PSScriptRoot 'bootstrap.ps1') -WithInstaller:$Installer}
& (Join-Path $PSScriptRoot 'build-brand.ps1')
$output=Join-Path $root ('dist\Translate-'+$Version+'-Windows-x64')
& (Join-Path $root 'src\build.ps1') -OutputDirectory $output
Copy-Item (Join-Path $root 'LICENSE') (Join-Path $output 'LICENSE.txt') -Force
Copy-Item (Join-Path $root 'THIRD_PARTY_NOTICES.md') (Join-Path $output 'THIRD_PARTY_NOTICES.md') -Force
Copy-Item (Join-Path $root 'docs\QUICK_START.txt') (Join-Path $output 'README.txt') -Force
Copy-Item (Join-Path $root 'licenses') $output -Recurse -Force
$stage=Join-Path $root '.cache\package\Translate'
if(Test-Path $stage){$resolved=[IO.Path]::GetFullPath($stage);if(!$resolved.StartsWith([IO.Path]::GetFullPath((Join-Path $root '.cache\package'))+[IO.Path]::DirectorySeparatorChar)){throw 'Unsafe staging path'};Remove-Item -LiteralPath $resolved -Recurse -Force}
New-Item -ItemType Directory -Path $stage -Force|Out-Null
foreach($item in @('Translate.exe','Translate.Launcher.exe','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.Wpf.dll','WebView2Loader.dll','README.txt','LICENSE.txt','THIRD_PARTY_NOTICES.md','licenses')){Copy-Item -LiteralPath (Join-Path $output $item) -Destination $stage -Recurse -Force}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $output 'offline-runtime') -Recurse -File){if($file.FullName-match '__pycache__|\.pyc$'){continue};$relative=[IO.Path]::GetRelativePath((Join-Path $output 'offline-runtime'),$file.FullName);$destination=Join-Path (Join-Path $stage 'offline-runtime') $relative;New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force|Out-Null;Copy-Item -LiteralPath $file.FullName -Destination $destination}
Compress-Archive -LiteralPath $stage -DestinationPath (Join-Path $root ('dist\Translate-'+$Version+'-Portable-x64.zip')) -Force
if($Installer){
 & (Join-Path $root '.tools\inno\ISCC.exe') /Q ('/DAppVersion='+$Version) (Join-Path $root 'installer\Translate.iss')
 if($LASTEXITCODE-ne 0){throw 'Installer compilation failed'}
}
$checksums=Get-ChildItem (Join-Path $root 'dist') -File | Where-Object {$_.Extension-in '.exe','.zip'} | Sort-Object Name | ForEach-Object {((Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())+'  '+$_.Name}
Set-Content -LiteralPath (Join-Path $root 'dist\SHA256SUMS.txt') -Value $checksums -Encoding ascii
Write-Output ('Release artifacts: '+(Join-Path $root 'dist'))
