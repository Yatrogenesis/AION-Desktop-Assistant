; 🤖 AION Desktop Assistant - Windows x64 Installer
; Inno Setup Script

#define MyAppName "AION Desktop Assistant"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "AION Technologies"
#define MyAppURL "https://github.com/Yatrogenesis/AION-Desktop-Assistant"
#define MyAppExeName "AionDesktopAssistant.exe"

[Setup]
AppId={{B5A8C9D0-3E1F-4A2B-8C9D-0E1F2A3B4C5D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir=.
OutputBaseFilename=AION-Desktop-Assistant-{#MyAppVersion}-Setup-x64
SetupIconFile=..\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; 🎨 Visual
WizardImageFile=..\assets\wizard-image.bmp
WizardSmallImageFile=..\assets\wizard-small.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode
Name: "startupicon"; Description: "Launch at Windows startup"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\artifacts\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\BIDIRECTIONAL-INTEGRATION.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\EMOJI_STYLE_GUIDE.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\claude-aion-cli.js"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:ProgramOnTheWeb,{#MyAppName}}"; Filename: "{#MyAppURL}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  MsgBox('🤖 Welcome to AION Desktop Assistant!' + #13#10 + #13#10 +
         'AI-Powered Accessibility for Everyone' + #13#10 + #13#10 +
         'This will install:' + #13#10 +
         '✅ Voice recognition & synthesis' + #13#10 +
         '✅ OCR screen reading' + #13#10 +
         '✅ Mouse & keyboard automation' + #13#10 +
         '✅ Claude Code AI integration' + #13#10 + #13#10 +
         'Click OK to continue...', mbInformation, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MsgBox('✅ Installation Complete!' + #13#10 + #13#10 +
           'Next steps:' + #13#10 +
           '1. Configure your microphone' + #13#10 +
           '2. Test voice commands' + #13#10 +
           '3. Explore Claude integration' + #13#10 + #13#10 +
           'Documentation: ' + #13#10 +
           '{#MyAppURL}', mbInformation, MB_OK);
  end;
end;
