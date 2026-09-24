#define AppVersion "1.1.1"
#define PublishDir "..\artifacts\publish\Caro"

[Setup]
AppId={{F5839222-74BC-465D-986C-8962797E4FF1}
AppName=Caro
AppVersion={#AppVersion}
AppVerName=C A R O {#AppVersion}
AppPublisher=Group 03
DefaultDirName={localappdata}\Programs\Caro
DefaultGroupName=Caro
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\artifacts\installer
OutputBaseFilename=CaroSetup
SetupIconFile=..\Code\UDM_16_CaroGame\CaroClient\Assets\Branding\caro.ico
UninstallDisplayIcon={app}\Caro.exe
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
CloseApplicationsFilter=Caro.exe,CaroServer.exe
RestartApplications=no
UninstallDisplayName=Caro
VersionInfoProductName=Caro
VersionInfoDescription=Caro Setup

[Tasks]
Name: "desktopicon"; Description: "Create a Caro desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "server-config.json,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishDir}\server-config.json"; DestDir: "{app}"; Flags: onlyifdoesntexist uninsneveruninstall

[Icons]
Name: "{group}\Caro"; Filename: "{app}\Caro.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Caro.exe"
Name: "{autodesktop}\Caro"; Filename: "{app}\Caro.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Caro.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Caro.exe"; Description: "Open C A R O"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\Server\CaroServer.exe"; Parameters: "--stop"; Flags: runhidden waituntilterminated; RunOnceId: "StopLocalCaroServer"
