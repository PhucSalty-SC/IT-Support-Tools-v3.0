; ============================================================
;  Inno Setup Script - IT Support Tools v3.0
; ============================================================

#define AppName      "IT Support Tools"
#define AppVersion   "3.0"
#define AppPublisher "IT Team"
#define AppExeName   "ITSupportToolkit.exe"
#define AppID        "{{B1C2D3E4-F5A6-7890-BCDE-F12345678901}"
#define SourceDir    "bin\Debug"

[Setup]
AppId                    = {#AppID}
AppName                  = {#AppName}
AppVersion               = {#AppVersion}
AppPublisher             = {#AppPublisher}
AppPublisherURL          = https://github.com/
AppSupportURL            = https://github.com/
AppUpdatesURL            = https://github.com/
DefaultDirName           = {autopf}\{#AppName}
DefaultGroupName         = {#AppName}
OutputDir                = installer_output
OutputBaseFilename       = ITSupportTools_Setup_v{#AppVersion}
SetupIconFile            = logo.ico
UninstallDisplayIcon     = {app}\{#AppExeName}
WizardImageFile          = WizardImage.bmp
WizardSmallImageFile     = WizardSmallImage.bmp
Compression              = lzma2/ultra64
SolidCompression         = yes
ArchitecturesInstallIn64BitMode = x64compatible
PrivilegesRequired       = admin
WizardStyle              = modern
DisableWelcomePage       = no
DisableDirPage           = no
DisableProgramGroupPage  = yes
AlwaysShowDirOnReadyPage = yes
MinVersion               = 10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create Desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
; File EXE chinh
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; Logo ICO - copy vao thu muc app de hien thi trong phan mem
Source: "logo.ico"; DestDir: "{app}"; Flags: ignoreversion
; README
Source: "README.txt"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "C:\IT_Tools"; Permissions: users-full
Name: "C:\IT_Tools\SDIO"; Permissions: users-full
Name: "C:\IT_Tools\ODT"; Permissions: users-full

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\logo.ico"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\logo.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch IT Support Tools"; Flags: nowait postinstall skipifsilent runascurrentuser

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function IsDotNetInstalled(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
end;

function InitializeSetup(): Boolean;
var
  MsgResult: Integer;
begin
  Result := True;
  if not IsDotNetInstalled() then
  begin
    MsgResult := MsgBox('.NET Framework 4.8 is required. Click Yes to download it from Microsoft.', mbConfirmation, MB_YESNO);
    if MsgResult = IDYES then
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet-framework/net48', '', '', SW_SHOW, ewNoWait, MsgResult);
    Result := False;
  end;
end;

procedure InitializeWizard();
begin
  WizardForm.Caption := 'Install IT Support Tools v3.0';
end;
