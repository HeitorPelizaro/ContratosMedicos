#!/usr/bin/env bash
# Gera o instalador Windows a partir do Linux. Um comando, sem etapa manual.
set -euo pipefail

VERSAO="${1:-1.0.0}"
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SAIDA="$RAIZ/artefatos"
PUBLICACAO="$SAIDA/publicacao"

echo "==> Rodando os testes antes de empacotar"
dotnet test "$RAIZ/ContratosMedicos.sln" --nologo

echo "==> Publicando single-file self-contained win-x64 (versão $VERSAO)"
rm -rf "$PUBLICACAO"
dotnet publish "$RAIZ/src/App/App.csproj" \
    -c Release -r win-x64 --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:DebugType=none \
    -p:Version="$VERSAO" \
    -o "$PUBLICACAO" --nologo

test -f "$PUBLICACAO/ContratosMedicos.exe" \
    || { echo "ERRO: o executável não foi gerado"; exit 1; }

echo "==> Montando o instalador NSIS"
makensis -DVERSAO="$VERSAO" "$RAIZ/empacotamento/instalador.nsi"

INSTALADOR="$SAIDA/Instalador-ContratosMedicos-$VERSAO.exe"
test -f "$INSTALADOR" || { echo "ERRO: o instalador não foi gerado"; exit 1; }

echo
echo "Pronto:"
ls -lh "$PUBLICACAO/ContratosMedicos.exe" "$INSTALADOR"
echo
echo "Envie para a usuária APENAS: $INSTALADOR"
