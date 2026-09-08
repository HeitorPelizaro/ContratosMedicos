; Instalador por-usuário: não pede senha de administrador, o que importa
; num computador gerenciado pela TI do hospital.

Unicode true
!include "LogicLib.nsh"
!define NOME "Controle de Contratos Médicos"
!define PASTA "ContratosMedicos"
!define EXECUTAVEL "ContratosMedicos.exe"
; Mesmo nome do Mutex de instância única do Program.cs — é assim que o instalador
; descobre que o programa está aberto antes de tentar sobrescrever o .exe.
!define MUTEX "ContratosMedicos.InstanciaUnica"

Name "${NOME}"
OutFile "..\artefatos\Instalador-ContratosMedicos-${VERSAO}.exe"
; Programa e dados em pastas DIFERENTES. Antes os dois moravam em
; $LOCALAPPDATA\ContratosMedicos — a mesma pasta que CaminhosApp usa para dados.db,
; anexos\, backups\ e log\ — e qualquer limpeza de pasta levava o banco junto.
; $LOCALAPPDATA\Programs\<app> é a raiz convencional de instalação por-usuário.
InstallDir "$LOCALAPPDATA\Programs\${PASTA}"
RequestExecutionLevel user
SetCompressor /SOLID lzma
BrandingText "${NOME} ${VERSAO}"
Icon "icone.ico"
UninstallIcon "icone.ico"

Function .onInit
    ; Sobrescrever o .exe com o programa aberto falha de um jeito confuso ("acesso
    ; negado" no meio da instalação). Melhor pedir para fechar antes.
    System::Call 'kernel32::CreateMutex(p 0, i 0, t "${MUTEX}") p .r0 ?e'
    Pop $1
    System::Call 'kernel32::CloseHandle(p r0)'
    ${If} $1 == 183   ; ERROR_ALREADY_EXISTS
        MessageBox MB_OK|MB_ICONEXCLAMATION \
            "O ${NOME} está aberto.$\r$\n$\r$\nFeche o programa (e a aba do navegador) \
e rode este instalador de novo."
        Abort
    ${EndIf}
FunctionEnd

Page directory
Page instfiles
UninstPage uninstConfirm
UninstPage instfiles

Section "Programa"
    SetOutPath "$INSTDIR"

    ; Só o programa é sobrescrito. A pasta de dados da usuária é outra
    ; ($LOCALAPPDATA\${PASTA}: dados.db, anexos\, backups\, log\) e não é tocada aqui.
    File "..\artefatos\publicacao\${EXECUTAVEL}"
    File "..\artefatos\publicacao\appsettings.json"
    File /r "..\artefatos\publicacao\wwwroot"
    File "icone.ico"

    CreateShortcut "$DESKTOP\${NOME}.lnk" "$INSTDIR\${EXECUTAVEL}" "" "$INSTDIR\icone.ico"
    CreateDirectory "$SMPROGRAMS\${NOME}"
    CreateShortcut "$SMPROGRAMS\${NOME}\${NOME}.lnk" "$INSTDIR\${EXECUTAVEL}" "" "$INSTDIR\icone.ico"
    CreateShortcut "$SMPROGRAMS\${NOME}\Desinstalar.lnk" "$INSTDIR\Desinstalar.exe"

    WriteUninstaller "$INSTDIR\Desinstalar.exe"

    ; Versões anteriores instalavam o programa DENTRO da pasta de dados. Se sobrou algo
    ; lá, limpamos só os arquivos do programa — dados.db, anexos\, backups\ e log\ ficam.
    ${If} $INSTDIR != "$LOCALAPPDATA\${PASTA}"
        Delete "$LOCALAPPDATA\${PASTA}\${EXECUTAVEL}"
        Delete "$LOCALAPPDATA\${PASTA}\appsettings.json"
        Delete "$LOCALAPPDATA\${PASTA}\icone.ico"
        Delete "$LOCALAPPDATA\${PASTA}\Desinstalar.exe"
        RMDir /r "$LOCALAPPDATA\${PASTA}\wwwroot"
    ${EndIf}

    ; Aparece em "Aplicativos instalados" do Windows.
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PASTA}" \
        "DisplayName" "${NOME}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PASTA}" \
        "DisplayVersion" "${VERSAO}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PASTA}" \
        "DisplayIcon" "$INSTDIR\icone.ico"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PASTA}" \
        "UninstallString" "$INSTDIR\Desinstalar.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PASTA}" \
        "NoModify" "1"
SectionEnd

Section "Uninstall"
    Delete "$INSTDIR\${EXECUTAVEL}"
    Delete "$INSTDIR\appsettings.json"
    RMDir /r "$INSTDIR\wwwroot"
    Delete "$INSTDIR\icone.ico"
    Delete "$INSTDIR\Desinstalar.exe"
    RMDir "$INSTDIR"

    Delete "$DESKTOP\${NOME}.lnk"
    Delete "$SMPROGRAMS\${NOME}\${NOME}.lnk"
    Delete "$SMPROGRAMS\${NOME}\Desinstalar.lnk"
    RMDir "$SMPROGRAMS\${NOME}"

    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PASTA}"

    ; Os dados da usuária NÃO são apagados de propósito, e agora por desenho e não por
    ; acidente: eles moram em %LOCALAPPDATA%\ContratosMedicos, que não é $INSTDIR.
SectionEnd
