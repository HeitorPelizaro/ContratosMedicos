using ClosedXML.Excel;
using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao;

/// <summary>
/// Gera o .xlsx que a Angélica manda por e-mail. Mantém os nomes de coluna da planilha
/// original, para quem recebe reconhecer o relatório.
/// </summary>
public static class ExportadorExcel
{
    private static readonly string[] Cabecalhos =
    {
        "PROFISSIONAL", "EMPRESA", "CNPJ", "OPTANTE SIMPLES NACIONAL", "ESPECIALIDADE",
        "AREA DE ATUAÇÃO", "EDITAL", "CÓDIGO CONTRATO TASY", "CONTRATO ORIGINAL",
        "DATA DE ASSINATURA DO CONTRATO ORIGINAL", "VIGÊNCIA", "SITUAÇÃO",
        "SITUAÇÃO DO VENCIMENTO", "ADITIVOS", "ADITIVOS PENDENTES", "OBSERVAÇÃO",
    };

    public static string NomeSugerido(DateOnly hoje) => $"Contratos-{hoje:yyyy-MM-dd}.xlsx";

    public static byte[] Gerar(IEnumerable<Contrato> contratos, DateOnly hoje, int diasAlerta)
    {
        using var pasta = new XLWorkbook();
        IXLWorksheet aba = pasta.Worksheets.Add("CONTRATOS");

        for (int c = 0; c < Cabecalhos.Length; c++)
        {
            IXLCell celula = aba.Cell(1, c + 1);
            celula.Value = Cabecalhos[c];
            celula.Style.Font.Bold = true;
            celula.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 239, 241, 244);
        }

        int linha = 2;
        foreach (Contrato contrato in contratos)
        {
            DateOnly? vigencia = RegrasContrato.VigenciaEfetiva(contrato);

            aba.Cell(linha, 1).Value = contrato.Profissional ?? "";
            aba.Cell(linha, 2).Value = contrato.Empresa ?? "";
            aba.Cell(linha, 3).Value = contrato.Cnpj ?? "";
            aba.Cell(linha, 4).Value = contrato.OptanteSimplesNacional switch
            {
                true => "SIM", false => "NÃO", _ => "",
            };
            aba.Cell(linha, 5).Value = contrato.Especialidade ?? "";
            aba.Cell(linha, 6).Value = contrato.AreaAtuacao ?? "";
            aba.Cell(linha, 7).Value = contrato.Edital ?? "";
            aba.Cell(linha, 8).Value = contrato.CodigoContratoTasy ?? "";
            aba.Cell(linha, 9).Value = contrato.ContratoOriginal ?? "";

            if (contrato.DataAssinaturaOriginal is DateOnly assinatura)
            {
                aba.Cell(linha, 10).Value = assinatura.ToDateTime(TimeOnly.MinValue);
                aba.Cell(linha, 10).Style.DateFormat.Format = "dd/MM/yyyy";
            }

            if (vigencia is DateOnly fim)
            {
                aba.Cell(linha, 11).Value = fim.ToDateTime(TimeOnly.MinValue);
                aba.Cell(linha, 11).Style.DateFormat.Format = "dd/MM/yyyy";
            }

            aba.Cell(linha, 12).Value = DescricoesSituacao.Rotulo(contrato.Situacao);
            aba.Cell(linha, 13).Value = TextoDoVencimento(contrato, hoje, diasAlerta);
            aba.Cell(linha, 14).Value = contrato.Aditivos.Count;
            aba.Cell(linha, 15).Value = contrato.Aditivos.Count(RegrasContrato.AditivoEstaPendente);
            aba.Cell(linha, 16).Value = contrato.Observacao ?? "";

            // Marcador de situação na primeira célula, como a tarja da tela.
            aba.Cell(linha, 1).Style.Fill.BackgroundColor =
                XLColor.FromHtml(DescricoesSituacao.Cor(contrato.Situacao));
            aba.Cell(linha, 1).Style.Font.FontColor = XLColor.White;

            linha++;
        }

        aba.SheetView.FreezeRows(1);
        aba.RangeUsed()?.SetAutoFilter();
        aba.Columns().AdjustToContents(1, 200);

        using var memoria = new MemoryStream();
        pasta.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static string TextoDoVencimento(Contrato contrato, DateOnly hoje, int diasAlerta)
    {
        int? dias = RegrasContrato.DiasParaVencer(contrato, hoje);
        return RegrasContrato.ClassificarVigencia(contrato, hoje, diasAlerta) switch
        {
            ClassificacaoVigencia.Vencida => $"Vencido há {Math.Abs(dias!.Value)} dia(s)",
            ClassificacaoVigencia.AVencer when dias == 0 => "Vence hoje",
            ClassificacaoVigencia.AVencer => $"Vence em {dias} dia(s)",
            ClassificacaoVigencia.Vigente => "Em vigência",
            _ => "Sem data de vigência",
        };
    }
}
