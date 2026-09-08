using ClosedXML.Excel;
using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao;

/// <summary>
/// Lê .xlsx/.xlsm com ClosedXML. A cor de fundo da linha é a situação: o mapa cor->situação
/// vem da própria aba LEGENDA CORES da planilha, em vez de hexadecimais chumbados no código.
/// </summary>
public class LeitorXlsx : ILeitorPlanilha
{
    private static readonly string[] Extensoes = { ".xlsx", ".xlsm" };
    private const string NomeAbaLegenda = "LEGENDA";
    private const string NomeAbaDados = "GERAL";

    public bool Aceita(string caminho) =>
        Extensoes.Contains(Path.GetExtension(caminho).ToLowerInvariant());

    public PlanilhaLida Ler(string caminho)
    {
        using var pasta = new XLWorkbook(caminho);

        Dictionary<string, Situacao> mapaCores = LerLegenda(pasta);
        IXLWorksheet dados = EscolherAbaDeDados(pasta);

        var cabecalhos = new List<string>();
        IXLRow? primeira = dados.FirstRowUsed();
        if (primeira is null)
            return new PlanilhaLida(cabecalhos, Array.Empty<LinhaPlanilha>(), mapaCores, Array.Empty<string>());

        int ultimaColuna = primeira.LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (int c = 1; c <= ultimaColuna; c++)
            cabecalhos.Add(primeira.Cell(c).GetFormattedString().Trim());

        var linhas = new List<LinhaPlanilha>();
        var coresSemLegenda = new List<string>();
        int ultimaLinha = dados.LastRowUsed()?.RowNumber() ?? primeira.RowNumber();

        for (int r = primeira.RowNumber() + 1; r <= ultimaLinha; r++)
        {
            IXLRow linha = dados.Row(r);
            var celulas = new List<string>(ultimaColuna);
            for (int c = 1; c <= ultimaColuna; c++)
                celulas.Add(linha.Cell(c).GetFormattedString().Trim());

            if (celulas.All(string.IsNullOrWhiteSpace)) continue;

            string? chaveCor = ChaveDaCor(linha, ultimaColuna);
            if (chaveCor is not null && !mapaCores.ContainsKey(chaveCor) && !coresSemLegenda.Contains(chaveCor))
                coresSemLegenda.Add(chaveCor);

            linhas.Add(new LinhaPlanilha(r, celulas, chaveCor));
        }

        return new PlanilhaLida(cabecalhos, linhas, mapaCores, coresSemLegenda);
    }

    private static IXLWorksheet EscolherAbaDeDados(XLWorkbook pasta)
    {
        IXLWorksheet? geral = pasta.Worksheets.FirstOrDefault(
            a => Texto.Normalizar(a.Name).Contains(NomeAbaDados, StringComparison.Ordinal));
        if (geral is not null) return geral;

        // Sem aba GERAL: a primeira que não é a legenda.
        return pasta.Worksheets.FirstOrDefault(
                   a => !Texto.Normalizar(a.Name).Contains(NomeAbaLegenda, StringComparison.Ordinal))
               ?? pasta.Worksheet(1);
    }

    private static Dictionary<string, Situacao> LerLegenda(XLWorkbook pasta)
    {
        var mapa = new Dictionary<string, Situacao>();
        IXLWorksheet? legenda = pasta.Worksheets.FirstOrDefault(
            a => Texto.Normalizar(a.Name).Contains(NomeAbaLegenda, StringComparison.Ordinal));
        if (legenda is null) return mapa;

        int ultima = legenda.LastRowUsed()?.RowNumber() ?? 0;
        for (int r = 1; r <= ultima; r++)
        {
            IXLCell celula = legenda.Cell(r, 1);
            Situacao? situacao = ReconhecedorLegenda.Reconhecer(celula.GetFormattedString());
            if (situacao is null) continue;

            string? chave = ChaveDaCor(celula);
            if (chave is not null) mapa[chave] = situacao.Value;
        }

        return mapa;
    }

    /// <summary>Chave estável da cor de fundo. Cobre cor direta, cor de tema e cor indexada.</summary>
    private static string? ChaveDaCor(IXLCell celula)
    {
        XLColor cor = celula.Style.Fill.BackgroundColor;
        return cor.ColorType switch
        {
            XLColorType.Color when cor.Color.A == 0 => null,
            XLColorType.Color => $"rgb:{cor.Color.R:X2}{cor.Color.G:X2}{cor.Color.B:X2}",
            XLColorType.Theme => $"tema:{cor.ThemeColor}:{cor.ThemeTint:0.###}",
            XLColorType.Indexed when cor.Indexed == 64 => null, // 64 = "sem preenchimento"
            XLColorType.Indexed => $"indexado:{cor.Indexed}",
            _ => null,
        };
    }

    /// <summary>Cor da linha: a primeira cor encontrada nas células usadas da linha.</summary>
    private static string? ChaveDaCor(IXLRow linha, int ultimaColuna)
    {
        for (int c = 1; c <= ultimaColuna; c++)
        {
            string? chave = ChaveDaCor(linha.Cell(c));
            if (chave is not null) return chave;
        }
        return null;
    }
}
