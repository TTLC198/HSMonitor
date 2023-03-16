#define AppName "HSMonitor"
#define AppVersion "1.1.0"
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
WizardStyle=modern
AllowNoIcons=yes
DisableWelcomePage=no
DisableProgramGroupPage=no
DisableReadyPage=no
UsePreviousAppDir=yes
SetupIconFile=..\.assets\favicon.ico
UninstallDisplayIcon={app}\{#AppName}.exe
LicenseFile=..\License.txt
OutputDir=bin\
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=HSMonitor-Installer
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: en; MessagesFile: "compiler:Default.isl"
Name: ru; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
en.StartupDescription="Start application when user logs in"
en.MinimizeDescription="Start application minimized"
ru.StartupDescription="Запуск приложения при входе пользователя в систему"
ru.MinimizeDescription="Запуск приложения свернутым"

[Files]
Source: "..\License.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\HSMonitor\appsettings.json"; DestDir: "{localappdata}\{#AppName}"; Flags: ignoreversion
Source: "Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppName}.exe"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{group}\{#AppName} on Github"; Filename: "{#GithubPage}"

[Tasks]
Name: startup; Description: "{cm:StartupDescription}"; GroupDescription: "Startup"
Name: minimize; Description: "{cm:MinimizeDescription}"; GroupDescription: "Startup"

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#AppName}"; ValueData: "{app}\{#AppName}"; \
  Tasks: startup

Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#AppName}"; ValueData: "{app}\{#AppName} --minimize"; \
  Tasks: minimize

[Run]
Filename: "{app}\{#AppName}.exe"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[UninstallDelete]
Name: "{autoappdata}\HSMonitor"; Type: filesandordirs