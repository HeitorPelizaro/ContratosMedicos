namespace ContratosMedicos.Dominio.Testes;

public class TextoDoIndicadorTeste
{
    private static IndicadoresMes Indicadores() => CalculadoraIndicadores.Calcular(new[]
    {
        new Contrato { Profissional = "Dra. Angélica", Especialidade = "Cardiologia",
            DataAssinaturaOriginal = new DateOnly(2026, 7, 10) },
        new Contrato { Profissional = "Dr. Bruno", Especialidade = "Ortopedia" },
    }, 2026, 7);

    [Fact]
    public void Texto_tem_o_mes_os_numeros_e_o_percentual()
    {
        string texto = TextoDoIndicador.ParaEmail(Indicadores());

        Assert.Contains("julho de 2026", texto);
        Assert.Contains("Médicos: 2", texto);
        Assert.Contains("Contratos assinados: 1", texto);
        Assert.Contains("50", texto);
    }

    [Fact]
    public void Texto_tem_a_quebra_por_especialidade()
    {
        string texto = TextoDoIndicador.ParaEmail(Indicadores());

        Assert.Contains("Cardiologia", texto);
        Assert.Contains("Ortopedia", texto);
    }

    [Fact]
    public void Sem_contrato_o_texto_nao_mostra_percentual_falso()
    {
        string texto = TextoDoIndicador.ParaEmail(
            CalculadoraIndicadores.Calcular(Array.Empty<Contrato>(), 2026, 7));

        Assert.DoesNotContain("0,0%", texto);
        Assert.Contains("—", texto);
    }
}
