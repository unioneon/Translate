#ifndef AppVersion
 #define AppVersion "1.1.0"
#endif

[Setup]
AppId={{DE218F9E-CED5-4AD4-99DA-45507C53DA0C}
AppName=Translate
AppVersion={#AppVersion}
AppVerName=Translate {#AppVersion}
AppPublisher=unioneon
AppPublisherURL=https://github.com/unioneon/Translate
AppSupportURL=https://github.com/unioneon/Translate/issues
AppUpdatesURL=https://github.com/unioneon/Translate/releases
DefaultDirName={localappdata}\Programs\Translate
DefaultGroupName=Translate
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\dist
OutputBaseFilename=Translate-{#AppVersion}-Setup-x64
SetupIconFile=..\src\assets\app.ico
UninstallDisplayIcon={app}\Translate.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
WizardImageFile=..\docs\design\wizard-large.bmp
WizardSmallImageFile=..\docs\design\wizard-small.bmp
WizardImageStretch=yes
LicenseFile=..\LICENSE
InfoBeforeFile=welcome.txt
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#AppVersion}.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\.cache\package\Translate\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "settings.dat,WebView2Data\*,offline-models\*"
Source: "..\.tools\MicrosoftEdgeWebview2Setup.exe"; Flags: dontcopy

[Icons]
Name: "{autoprograms}\Translate"; Filename: "{app}\Translate.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Translate"; Filename: "{app}\Translate.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; Flags: runhidden waituntilterminated; Check: NeedsWebView
Filename: "{app}\Translate.exe"; Description: "Launch Translate"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\Translate.exe"; Parameters: "--shutdown"; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "StopTranslate"
Filename: "{app}\Translate.Launcher.exe"; Parameters: "--uninstall"; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "StopTranslateLauncher"

[Code]
function NeedsWebView: Boolean;
var Version: String;
begin
 Result := True;
 if RegQueryStringValue(HKLM32, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) then
  if (Version <> '') and (Version <> '0.0.0.0') then Result := False;
 if RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) then
  if (Version <> '') and (Version <> '0.0.0.0') then Result := False;
end;

function InitializeSetup: Boolean;
var Release: Cardinal;
begin
 Result := RegQueryDWordValue(HKLM32, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
 if not Result then MsgBox('Translate requires .NET Framework 4.8 or newer. Install it from Microsoft, then run Setup again.', mbInformation, MB_OK);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var Code: Integer;
begin
 Result := '';
 if NeedsWebView then ExtractTemporaryFile('MicrosoftEdgeWebview2Setup.exe');
 if FileExists(ExpandConstant('{app}\Translate.exe')) then
  Exec(ExpandConstant('{app}\Translate.exe'), '--shutdown', '', SW_HIDE, ewWaitUntilTerminated, Code);
 if FileExists(ExpandConstant('{app}\Translate.Launcher.exe')) then
  Exec(ExpandConstant('{app}\Translate.Launcher.exe'), '--uninstall', '', SW_HIDE, ewWaitUntilTerminated, Code);
end;
