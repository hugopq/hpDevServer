#define AppName "hpDevServer"
#define AppVersion "1.1"
#define AppPublisher "Hugo Perdigão"
#define AppExeName "ServerManager.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={userdocs}\hpDevServer
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=hpDevServer_Setup_v{#AppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\hpdev.ico
UninstallDisplayIcon={app}\{#AppExeName}
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docker-compose.yml";    DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Atalho na pasta de arranque do Windows
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#AppExeName}"

[Run]
; Inicia a aplicação no fim da instalação (opcional)
Filename: "{app}\{#AppExeName}"; Description: "Iniciar {#AppName} agora"; Flags: postinstall nowait skipifsilent

[Code]

// ── Verificar WSL2 ──────────────────────────────────────────────────────────
function IsWSL2Installed: Boolean;
var
  Dummy: String;
begin
  // O serviço LxssManager só existe quando WSL2 está instalado e ativado
  Result :=
    FileExists(ExpandConstant('{sys}\wsl.exe')) and
    RegQueryStringValue(HKLM, 'SYSTEM\CurrentControlSet\Services\LxssManager', 'ImagePath', Dummy);
end;

// ── Verificar Docker Desktop ────────────────────────────────────────────────
function IsDockerInstalled: Boolean;
var
  Dummy: String;
begin
  Result :=
    // Instalação de utilizador — path padrão do Docker Desktop moderno
    FileExists(ExpandConstant('{localappdata}\Programs\Docker\Docker\Docker Desktop.exe')) or
    // Instalação de sistema
    FileExists(ExpandConstant('{pf}\Docker\Docker\Docker Desktop.exe')) or
    // Chave de registo Docker Inc.
    RegKeyExists(HKCU, 'SOFTWARE\Docker Inc.\Docker Desktop') or
    RegKeyExists(HKLM, 'SOFTWARE\Docker Inc.\Docker Desktop') or
    RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Docker Desktop', 'DisplayName', Dummy) or
    RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Docker Desktop', 'DisplayName', Dummy);
end;

// ── Verificações antes de instalar ─────────────────────────────────────────
function InitializeSetup: Boolean;
var
  Missing:  String;
  MsgFull:  String;
begin
  Missing := '';

  if not IsWSL2Installed then
    Missing := Missing +
      '  • WSL2 (Windows Subsystem for Linux 2)' + #13#10 +
      '    Instala abrindo o PowerShell como administrador e correndo:' + #13#10 +
      '    wsl --install' + #13#10#13#10;

  if not IsDockerInstalled then
    Missing := Missing +
      '  • Docker Desktop' + #13#10 +
      '    Faz o download em: https://www.docker.com/products/docker-desktop' + #13#10#13#10;

  if Missing <> '' then
  begin
    MsgFull :=
      'Os seguintes pré-requisitos não foram encontrados:' + #13#10#13#10 +
      Missing +
      'A instalação vai continuar, mas o hpDevServer não funcionará' + #13#10 +
      'corretamente sem estes componentes instalados.';

    MsgBox(MsgFull, mbInformation, MB_OK);
  end;

  Result := True; // nunca bloqueia — apenas avisa
end;

// ── Mensagem final ──────────────────────────────────────────────────────────
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssDone then
  begin
    MsgBox(
      'Instalação concluída!' + #13#10#13#10 +
      'O hpDevServer foi adicionado ao arranque do Windows.' + #13#10 +
      'Da próxima vez que iniciares o PC, o gestor arranca automaticamente no system tray.',
      mbInformation, MB_OK
    );
  end;
end;
