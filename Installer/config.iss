#define AppName "HSMonitor"
#define AppVersion "1.0.6"
#define GithubPage "https://github.com/TTLC198/HSMonitor"

[Setup]
AppId={{85096BD0-07D6-4C8C-B8C1-D86DFD0073F7}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher="ttlc198"
AppPublisherURL="{#GithubPage}"
AppSupportURL="{#GithubPage}/issues"
AppUpdatesURL="{#GithubPage}/releases"
AppMutex=HSMonitor_Identity
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
DisableWelcomePage=yes
DisableProgramGroupPage=no
DisableReadyPage=yes
SetupIconFile=..\.assets\favicon.ico
UninstallDisplayIcon={app}\{#AppName}.exe
LicenseFile=..\License.txt
OutputDir=bin\
OutputBaseFilename=HSMonitor-Installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\License.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\HSMonitor\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppName}.exe"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{group}\{#AppName} on Github"; Filename: "{#GithubPage}"

[Run]
Filename: "{app}\{#AppName}.exe"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Name: "{userappdata}\HSMonitor"; Type: filesandordirs