using ClosedXML.Excel;
using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao.Testes;

public class ExportadorExcelTeste
{
    private static readonly DateOnly Hoje = new(2026, 8, 31);

    [Fact]
    public void Gera_planilha_com_cabecalho_e_uma_linha_por_contrato()
    {
        byte[] bytes = ExportadorExcel.Gerar(new[]
        {
            new Contrato
            {
                Profissional = "Dra. Angélica", Empresa = "Clínica Alfa", Especialidade = "Cardiologia",
                Cnpj = "12345678000199", CodigoContratoTasy = "TASY-1", Situacao = Situacao.Assinado,
                DataAssinaturaOriginal = new DateOnly(2026, 1, 10), VigenciaFim = new DateOnly(2026, 12, 31),
            },
        }, Hoje, 30);

        using var pasta = new XLWorkbook(new MemoryStream(bytes));
        IXLWorksheet aba = pasta.Worksheet(1);

        Assert.Equal("PROFISSIONAL", aba.Cell(1, 1).GetString().ToUpperInvariant());
        Assert.Equal("Dra. Angélica", aba.Cell(2, 1).GetString());
        Assert.Contains("31/12/2026", aba.Row(2).Cells().Select(c => c.GetFormattedString()));
    }

    [Fact]
    public void Traz_a_situacao_por_extenso_e_o_estado_do_vencimento()
    {
        byte[] bytes = ExportadorExcel.Gerar(new[]
        {
            new Contrato { Profissional = "Dr. B", Situacao = Situacao.AguardandoAssinaturaDigital,
                VigenciaFim = new DateOnly(2026, 7, 1) },
        }, Hoje, 30);

        using var pasta = new XLWorkbook(new MemoryStream(bytes));
        List<string> linha = pasta.Worksheet(1).Row(2).Cells().Select(c => c.GetString()).ToList();

        Assert.Contains("Aguardando assinatura digital", linha);
        Assert.Contains(linha, v => v.Contains("Vencid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Lista_vazia_gera_planilha_apenas_com_cabecalho()
    {
        byte[] bytes = ExportadorExcel.Gerar(Array.Empty<Contrato>(), Hoje, 30);

        using var pasta = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(1, pasta.Worksheet(1).LastRowUsed()!.RowNumber());
    }

    [Fact]
    public void Nome_sugerido_tem_a_data_de_hoje()
    {
        Assert.Equal("Contratos-2026-08-31.xlsx", ExportadorExcel.NomeSugerido(Hoje));
    }
}
