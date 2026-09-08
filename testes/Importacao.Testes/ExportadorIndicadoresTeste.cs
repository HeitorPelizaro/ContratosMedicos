using ClosedXML.Excel;
using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao.Testes;

public class ExportadorIndicadoresTeste
{
    private static IndicadoresMes Indicadores() => CalculadoraIndicadores.Calcular(new[]
    {
        new Contrato { Profissional = "Dra. Angélica", Especialidade = "Cardiologia",
            DataAssinaturaOriginal = new DateOnly(2026, 7, 10) },
        new Contrato { Profissional = "Dr. Bruno", Especialidade = "Ortopedia" },
    }, 2026, 7);

    [Fact]
    public void Planilha_tem_a_aba_de_resumo_com_os_numeros()
    {
        using var pasta = new XLWorkbook(new MemoryStream(ExportadorIndicadores.Gerar(Indicadores())));
        IXLWorksheet resumo = pasta.Worksheet("RESUMO");

        List<string> textos = resumo.CellsUsed().Select(c => c.GetFormattedString()).ToList();
        Assert.Contains(textos, t => t.Contains("julho de 2026"));
        Assert.Contains(textos, t => t.Contains("Médicos"));
        Assert.Contains("2", textos);
        Assert.Contains("1", textos);
    }

    [Fact]
    public void Planilha_tem_a_aba_por_especialidade()
    {
        using var pasta = new XLWorkbook(new MemoryStream(ExportadorIndicadores.Gerar(Indicadores())));
        IXLWorksheet aba = pasta.Worksheet("POR ESPECIALIDADE");

        Assert.Equal("ESPECIALIDADE", aba.Cell(1, 1).GetString());
        List<string> especialidades = aba.Column(1).CellsUsed().Select(c => c.GetString()).ToList();
        Assert.Contains("Cardiologia", especialidades);
        Assert.Contains("Ortopedia", especialidades);
    }

    [Fact]
    public void Nome_sugerido_tem_ano_e_mes()
    {
        Assert.Equal("Indicador-Contratos-2026-07.xlsx",
            ExportadorIndicadores.NomeSugerido(Indicadores()));
    }
}
