using ClosedXML.Excel;
using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao;

/// <summary>Planilha do indicador mensal: uma aba de resumo e uma por especialidade.</summary>
public static class ExportadorIndicadores
{
    public static string NomeSugerido(IndicadoresMes i) => $"Indicador-Contratos-{i.Ano}-{i.Mes:00}.xlsx";

    public static byte[] Gerar(IndicadoresMes i)
    {
        using var pasta = new XLWorkbook();

        IXLWorksheet resumo = pasta.Worksheets.Add("RESUMO");
        resumo.Cell(1, 1).Value = $"Indicador de contratos médicos — {i.NomeDoMes}";
        resumo.Cell(1, 1).Style.Font.Bold = true;
        resumo.Cell(2, 1).Value = $"Posição em {i.Corte:dd/MM/yyyy}";

        // Número e texto são atribuídos em ramos separados de propósito: evita depender de
        // XLCellValue.FromObject, cuja assinatura muda entre versões do ClosedXML.
        var numeros = new (string Rotulo, int Valor)[]
        {
            ("Médicos", i.MedicosDistintos),
            ("Contratos cadastrados", i.ContratosTotal),
            ("Contratos assinados", i.ContratosAssinadosAte),
            ("Médicos com contrato assinado", i.MedicosComContratoAssinado),
        };

        int linha = 4;
        foreach ((string rotulo, int valor) in numeros)
        {
            resumo.Cell(linha, 1).Value = rotulo;
            resumo.Cell(linha, 1).Style.Font.Bold = true;
            resumo.Cell(linha, 2).Value = valor;
            linha++;
        }

        resumo.Cell(linha, 1).Value = "Cobertura (%)";
        resumo.Cell(linha, 1).Style.Font.Bold = true;
        if (i.PercentualCobertura is decimal cobertura) resumo.Cell(linha, 2).Value = cobertura;
        else resumo.Cell(linha, 2).Value = "—";
        linha++;

        var doMes = new (string Rotulo, int Valor)[]
        {
            ("Contratos assinados no mês", i.ContratosAssinadosNoMes),
            ("Médicos envolvidos no mês", i.MedicosDistintosNoMes),
            ("Aditivos assinados no mês", i.AditivosAssinadosNoMes),
        };

        foreach ((string rotulo, int valor) in doMes)
        {
            resumo.Cell(linha, 1).Value = rotulo;
            resumo.Cell(linha, 1).Style.Font.Bold = true;
            resumo.Cell(linha, 2).Value = valor;
            linha++;
        }
        resumo.Columns().AdjustToContents(1, 100);

        IXLWorksheet porEspecialidade = pasta.Worksheets.Add("POR ESPECIALIDADE");
        string[] cabecalhos = { "ESPECIALIDADE", "MÉDICOS", "CONTRATOS", "CONTRATOS ASSINADOS",
                                "MÉDICOS COM CONTRATO ASSINADO" };
        for (int c = 0; c < cabecalhos.Length; c++)
        {
            IXLCell celula = porEspecialidade.Cell(1, c + 1);
            celula.Value = cabecalhos[c];
            celula.Style.Font.Bold = true;
            celula.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 239, 241, 244);
        }

        int atual = 2;
        foreach (LinhaIndicador item in i.PorEspecialidade)
        {
            porEspecialidade.Cell(atual, 1).Value = item.Rotulo;
            porEspecialidade.Cell(atual, 2).Value = item.Medicos;
            porEspecialidade.Cell(atual, 3).Value = item.Contratos;
            porEspecialidade.Cell(atual, 4).Value = item.ContratosAssinados;
            porEspecialidade.Cell(atual, 5).Value = item.MedicosComContratoAssinado;
            atual++;
        }

        porEspecialidade.SheetView.FreezeRows(1);
        porEspecialidade.Columns().AdjustToContents(1, 120);

        using var memoria = new MemoryStream();
        pasta.SaveAs(memoria);
        return memoria.ToArray();
    }
}
