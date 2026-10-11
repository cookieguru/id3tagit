; Installer for ID3-TagIT (Inno Setup 6). Build from the repo root after a Release build:
;   iscc /DSuffix=<name> /DSourceDir=<repo>\bin\Release installer\ID3-TagIT.iss
; It keeps the AppId of the 3.3.0 installer, so it upgrades a 3.3.0 install in place.

#ifndef SourceDir
  #define SourceDir "..\bin\Release"
#endif
#ifndef Suffix
  #define Suffix "local"
#endif
#define AppVersion GetVersionNumbersString(SourceDir + "\ID3-TagIT.exe")
#define UninstallKey "Software\Microsoft\Windows\CurrentVersion\Uninstall\ID3-TagIT 3_is1"
#define ShellKey "Software\Classes\Directory\shell\ID3-TagIT"

[Setup]
AppId=ID3-TagIT 3
AppName=ID3-TagIT 3
AppVersion={#AppVersion}
AppVerName=ID3-TagIT {#AppVersion}
AppPublisher=Michael Pluemper
AppPublisherURL=https://id3-tagit.de/
DefaultDirName={autopf}\ID3-TagIT 3
DefaultGroupName=ID3-TagIT 3
UsePreviousAppDir=yes
AllowNoIcons=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ShowLanguageDialog=yes
ChangesAssociations=no
WizardStyle=modern
SetupIconFile=..\ID3_TagIT.Logo.ico
UninstallDisplayIcon={app}\ID3-TagIT.exe
UninstallDisplayName=ID3-TagIT 3
Compression=lzma2
SolidCompression=yes
OutputDir=Output
OutputBaseFilename=ID3-TagIT-{#Suffix}-Setup

[Languages]
Name: "English"; MessagesFile: "compiler:Default.isl"; LicenseFile: "License.rtf"
Name: "Deutsch"; MessagesFile: "compiler:Languages\German.isl"; LicenseFile: "License-Ger.rtf"
Name: "Polski"; MessagesFile: "compiler:Languages\Polish.isl"; LicenseFile: "License.rtf"

[CustomMessages]
English.ShellMenu=Add ID3-TagIT to the folder right-click menu
Deutsch.ShellMenu=ID3-TagIT zum Kontextmenü von Ordnern hinzufügen
Polski.ShellMenu=Dodaj ID3-TagIT do menu kontekstowego folderów
English.DeleteSettings=Also delete your ID3-TagIT settings?
Deutsch.DeleteSettings=Sollen auch Ihre ID3-TagIT-Einstellungen gelöscht werden?
Polski.DeleteSettings=Czy usunąć również ustawienia ID3-TagIT?
English.OldInstallForAllUsers=ID3-TagIT 3 is already installed for all users. Installing it for you only adds a second copy.%n%nTo upgrade the existing copy, cancel and choose "Install for all users". Continue anyway?
Deutsch.OldInstallForAllUsers=ID3-TagIT 3 ist bereits für alle Benutzer installiert. Eine Installation nur für Sie legt eine zweite Kopie an.%n%nUm die vorhandene Kopie zu aktualisieren, brechen Sie ab und wählen "Für alle Benutzer installieren". Trotzdem fortfahren?
Polski.OldInstallForAllUsers=ID3-TagIT 3 jest już zainstalowany dla wszystkich użytkowników. Instalacja tylko dla Ciebie doda drugą kopię.%n%nAby zaktualizować istniejącą kopię, anuluj i wybierz instalację dla wszystkich użytkowników. Kontynuować mimo to?
English.NeedsDotNet=ID3-TagIT needs the Microsoft .NET Framework 4.8. Please install it from Microsoft and run this setup again.
Deutsch.NeedsDotNet=ID3-TagIT benötigt das Microsoft .NET Framework 4.8. Bitte installieren Sie es von Microsoft und starten Sie dieses Setup erneut.
Polski.NeedsDotNet=ID3-TagIT wymaga Microsoft .NET Framework 4.8. Zainstaluj go ze strony Microsoft i uruchom instalator ponownie.

[Tasks]
Name: "shellmenu"; Description: "{cm:ShellMenu}"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\ID3-TagIT.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\ID3-TagIT.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\ICSharpCode.SharpZipLib.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\Templetes\Readme.txt"; DestDir: "{app}\Templetes"; Flags: ignoreversion
; Users may have edited the templates; keep their copies.
Source: "{#SourceDir}\Templetes\*.itl"; DestDir: "{app}\Templetes"; Flags: onlyifdoesntexist

[InstallDelete]
; Left behind by 3.3.0 and no longer used.
Type: files; Name: "{app}\DevComponents.DotNetBar.dll"
Type: files; Name: "{app}\SkySoftware.FolderView.dll"
Type: files; Name: "{app}\AMS.TextBox.dll"
Type: files; Name: "{app}\ID3-TagIT-Ressources.xml"
Type: files; Name: "{app}\ID3-TagIT.exe.manifest"
Type: filesandordirs; Name: "{app}\Help"
Type: filesandordirs; Name: "{commonappdata}\ID3-TagIT 3"; Check: IsAdminInstallMode

[Registry]
; HKA is HKLM for an all-users install and HKCU for a current-user install.
Root: HKA; Subkey: "{#ShellKey}"; Flags: uninsdeletekey; Tasks: shellmenu
Root: HKA; Subkey: "{#ShellKey}\command"; ValueType: string; ValueName: ""; ValueData: """{app}\ID3-TagIT.exe"" ""/P=%1"""; Flags: uninsdeletekey; Tasks: shellmenu
; 3.3.0 added the entry under Folder; with the Directory entry above, folders would show it twice.
Root: HKLM; Subkey: "Software\Classes\Folder\shell\ID3-TagIT"; ValueType: none; Flags: deletekey; Check: IsAdminInstallMode

[Icons]
Name: "{group}\ID3-TagIT 3"; Filename: "{app}\ID3-TagIT.exe"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,ID3-TagIT 3}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ID3-TagIT 3"; Filename: "{app}\ID3-TagIT.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;

  if not IsDotNetInstalled(net48, 0) then
  begin
    SuppressibleMsgBox(CustomMessage('NeedsDotNet'), mbCriticalError, MB_OK, IDOK);
    Result := False;
    Exit;
  end;

  if not IsAdminInstallMode and RegKeyExists(HKLM32, '{#UninstallKey}') then
    Result := SuppressibleMsgBox(CustomMessage('OldInstallForAllUsers'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2, IDYES) = IDYES;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;

  // The current user's own entry or "hide" override, added from Preferences.
  RegDeleteKeyIncludingSubkeys(HKCU, '{#ShellKey}');

  if not UninstallSilent then
    if MsgBox(CustomMessage('DeleteSettings'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      DelTree(ExpandConstant('{userappdata}\ID3-TagIT 3'), True, True, True);
end;
