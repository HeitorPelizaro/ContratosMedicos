using ClosedXML.Excel;

namespace ContratosMedicos.Importacao.Testes;

/// <summary>Gera planilhas de fixture parecidas com a da usuária, inclusive as cores de linha.</summary>
public static class FabricaDePlanilhas
{
    public static readonly string[] CabecalhosDaPlanilhaReal =
    {
        "CADASTRO REGRAS E CRITÉRIOS / REPASSE HONORÁRIOS MÉDICOS",
        "CNPJ", "OPTANTE SIMPLES NACIONAL", "EMPRESA", "PROFISSIONAL",
        "ESPECIALIDADE", "AREA DE ATUAÇÃO", "EDITAL", "CÓDIGO CONTRATO TASY",
        "CONTRATO ORIGINAL", "DATA DE ASSINATURA DO CONTRATO ORIGINAL",
        "ADITIVO", "VIGÊNCIA",
    };

    public static readonly (string Rotulo, XLColor Cor)[] LegendaPadrao =
    {
        ("SEM CONTRATO", XLColor.FromArgb(255, 255, 255, 255)),
        ("CONTRATO COM PRAZO ATRASADO E SEM PRORROGAÇÃO (SEM PRORROGAÇÃO CONFECCIONADO)", XLColor.FromArgb(255, 255, 0, 0)),
        ("AGUARDANDO ASSINATURA INSTITUCIONAL", XLColor.FromArgb(255, 0, 112, 192)),
        ("COM PROBLEMA NO CONTRATO/ADITIVO", XLColor.FromArgb(255, 255, 153, 0)),
        ("DUVIDA", XLColor.FromArgb(255, 112, 48, 160)),
        ("AGUARDANDO ASSINATURA DO PROFISSIONAL", XLColor.FromArgb(255, 255, 255, 0)),
        ("T.A VIGENCIA FEITO - AGUARDANDO ASSINATURA", XLColor.FromArgb(255, 0, 176, 80)),
        ("AGUARDANDO ASSINATURA DIGITAL", XLColor.FromArgb(255, 0, 32, 96)),
        ("APENAS UM DOS TERMOS ADITIVOS ENVIADO", XLColor.FromArgb(255, 192, 0, 0)),
    };

    /// <param name="linhas">Cada linha: valores das células e, opcionalmente, a cor de fundo.</param>
    public static void CriarXlsx(
        string caminho,
        IEnumerable<(string[] Celulas, XLColor? Cor)> linhas,
        bool comAbaLegenda = true,
        string[]? cabecalhos = null)
    {
        using var pasta = new XLWorkbook();

        if (comAbaLegenda)
        {
            IXLWorksheet legenda = pasta.Worksheets.Add("LEGENDA CORES");
            for (int i = 0; i < LegendaPadrao.Length; i++)
            {
                legenda.Cell(i + 1, 1).Value = LegendaPadrao[i].Rotulo;
                legenda.Cell(i + 1, 1).Style.Fill.BackgroundColor = LegendaPadrao[i].Cor;
            }
        }

        IXLWorksheet geral = pasta.Worksheets.Add("GERAL");
        string[] titulos = cabecalhos ?? CabecalhosDaPlanilhaReal;
        for (int c = 0; c < titulos.Length; c++) geral.Cell(1, c + 1).Value = titulos[c];

        int numeroLinha = 2;
        foreach ((string[] celulas, XLColor? cor) in linhas)
        {
            for (int c = 0; c < celulas.Length; c++) geral.Cell(numeroLinha, c + 1).Value = celulas[c];
            if (cor is not null)
                geral.Row(numeroLinha).Cells(1, titulos.Length).Style.Fill.BackgroundColor = cor;
            numeroLinha++;
        }

        // SaveAs(caminho) valida a extensão do arquivo e rejeita, por exemplo, ".csv";
        // via Stream não há essa validação, o que é útil para simular extensão errada.
        using (FileStream saida = File.Create(caminho))
            pasta.SaveAs(saida);
    }
}
