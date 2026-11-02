; Inno Setup script for Keystone. tools\windows\package.ps1 stages the files and runs this.
;
; Installs for the current user, under %LOCALAPPDATA%\Programs, so it needs no administrator and
; the game can write its saves beside itself, which is where the engine keeps them.

#ifndef AppVersion
  #define AppVersion "dev"
#endif
#ifndef StageDir
  #error StageDir must be defined: run package.ps1
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif

[Setup]
AppId={{6B0C2F5E-2D7A-4C51-9E1B-8A3F4D7C2E90}
AppName=Keystone
AppVersion={#AppVersion}
AppVerName=Keystone {#AppVersion}
AppPublisher=theosib
AppPublisherURL=https://github.com/theosib/Keystone-RTS
AppSupportURL=https://github.com/theosib/Keystone-RTS/issues
DefaultDirName={autopf}\Keystone
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=Keystone-{#AppVersion}-setup
SetupIconFile={#StageDir}\keystone.ico
UninstallDisplayIcon={app}\keystone.ico
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
WizardStyle=modern
CloseApplications=yes

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#StageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Keystone"; Filename: "{app}\Keystone.exe"; WorkingDir: "{app}"; IconFilename: "{app}\keystone.ico"
Name: "{autodesktop}\Keystone"; Filename: "{app}\Keystone.exe"; WorkingDir: "{app}"; IconFilename: "{app}\keystone.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\Keystone.exe"; WorkingDir: "{app}"; Description: "{cm:LaunchProgram,Keystone}"; Flags: nowait postinstall skipifsilent

[Code]
// The game loads every file in its content directories, so one that a newer version no longer
// has must go before that version goes in. Saves stay.
procedure ClearOldContent();
var
  Dir: String;
  Found: TFindRec;
begin
  Dir := ExpandConstant('{app}\game');
  if not DirExists(Dir) then Exit;
  if FindFirst(Dir + '\*', Found) then begin
    try
      repeat
        if (Found.Name <> '.') and (Found.Name <> '..') and (CompareText(Found.Name, 'saves') <> 0) then begin
          if Found.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0 then
            DelTree(Dir + '\' + Found.Name, True, True, True)
          else
            DeleteFile(Dir + '\' + Found.Name);
        end;
      until not FindNext(Found);
    finally
      FindClose(Found);
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then ClearOldContent();
end;
