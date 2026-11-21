param([switch]$WithInstaller)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$src=Join-Path $root 'src'
$cache=Join-Path $root '.cache'
New-Item -ItemType Directory -Path $cache -Force | Out-Null
foreach($dependency in (Get-Content -Raw (Join-Path $PSScriptRoot 'dependencies.lock.json') | ConvertFrom-Json)){
 $archive=Join-Path $cache ($dependency.name+'.zip')
 if(!(Test-Path $archive)){Invoke-WebRequest -Uri $dependency.url -OutFile $archive}
 if((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $dependency.sha256){throw ('Dependency checksum mismatch: '+$dependency.name)}
 $destination=Join-Path $src $dependency.destination
 $stamp=Join-Path $cache ($dependency.name+'.installed')
 if(!(Test-Path $stamp)-or !(Test-Path $destination)){New-Item -ItemType Directory -Path $destination -Force | Out-Null;Expand-Archive -LiteralPath $archive -DestinationPath $destination -Force;Set-Content $stamp $dependency.sha256}
}
Set-Content -LiteralPath (Join-Path $src 'neural-runtime\python312._pth') -Value @('python312.zip','.','Lib\site-packages','import site') -Encoding ascii
Copy-Item (Join-Path $src 'translate_worker.py') (Join-Path $src 'neural-runtime\translate_worker.py') -Force
if($WithInstaller){
 $tools=Join-Path $root '.tools';New-Item -ItemType Directory -Path $tools -Force | Out-Null
 $compiler=Join-Path $tools 'inno\ISCC.exe'
 if(!(Test-Path $compiler)){
  $setup=Join-Path $cache 'innosetup-7.1.1-x64.exe'
  if(!(Test-Path $setup)){Invoke-WebRequest 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_1/innosetup-7.1.1-x64.exe' -OutFile $setup}
  if((Get-FileHash $setup).Hash.ToLowerInvariant() -ne '8a5a6711c3766a6e5fb74f280ba0e3551e719ce94538d901fdbd1f8f9920b3f0'){throw 'Inno Setup checksum mismatch'}
  $signature=Get-AuthenticodeSignature $setup;if($signature.Status-ne 'Valid'-or$signature.SignerCertificate.Subject-notmatch 'Pyrsys'){throw 'Unexpected compiler signature'}
  $innoDirectory=Join-Path $tools 'inno'
  $process=Start-Process $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/CURRENTUSER','/NOICONS',('/DIR="'+$innoDirectory+'"')) -WindowStyle Hidden -Wait -PassThru
  if($process.ExitCode-ne 0){throw 'Compiler installation failed'}
 }
 $bootstrapper=Join-Path $tools 'MicrosoftEdgeWebview2Setup.exe'
 if(!(Test-Path $bootstrapper)){Invoke-WebRequest 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile $bootstrapper}
 $signature=Get-AuthenticodeSignature $bootstrapper
 if($signature.Status-ne 'Valid'-or$signature.SignerCertificate.Subject-notmatch 'Microsoft Corporation'){throw 'Unexpected WebView2 bootstrapper signature'}
}
Write-Output 'Verified build dependencies are ready.'
