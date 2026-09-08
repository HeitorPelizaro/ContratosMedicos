# Controle de Contratos Médicos

Aplicativo Windows que substitui a planilha de controle de contratos médicos:
painel em tempo real (assinados, pendentes, vigência vencida, a vencer), cadastro de
contratos e aditivos, anexos em PDF, indicador mensal de médicos x contratos assinados para a
gerência (com foto do mês fechado), exportação para Excel e importação da planilha atual
(XLSX/XLSM/XLS/CSV, lendo as cores da legenda).

## Estrutura

- `src/Dominio` — entidades e regras (situação derivada, vigência efetiva, fila de atenção, indicador mensal). Sem dependências.
- `src/Dados` — EF Core + SQLite, caminhos em `%LOCALAPPDATA%`, backup diário, anexos.
- `src/Importacao` — leitores de planilha, mapeamento de colunas, validação, gravação, exportação.
- `src/App` — Blazor Server em Kestrel preso a `127.0.0.1`, aberto em navegador modo app.
- `testes/` — xUnit.
- `empacotamento/` — publicação single-file e instalador NSIS.

## Desenvolvimento

```bash
dotnet test                       # bateria completa
dotnet run --project src/App      # sobe o app; a URL fica em sessao-atual.txt na pasta de dados
```

## Gerar o instalador Windows (a partir do Linux)

```bash
sudo apt install -y nsis
./empacotamento/build-windows.sh 1.0.0
```

Sai em `artefatos/Instalador-ContratosMedicos-1.0.0.exe`. É esse — e só esse — arquivo que vai para a usuária.

## Documentos

- Design: `docs/superpowers/specs/2026-08-31-controle-contratos-medicos-design.md`
- Plano de implementação: `docs/superpowers/plans/2026-08-31-controle-contratos-medicos.md`
- Guia da usuária: `docs/guia-da-angelica.md`
