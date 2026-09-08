namespace ContratosMedicos.Dominio.Testes;

public class CalculadoraResumoTeste
{
    private static readonly DateOnly Hoje = new(2026, 8, 31);

    private static Contrato Contrato(Situacao situacao, DateOnly? assinatura, DateOnly? vigencia) => new()
    {
        Situacao = situacao,
        DataAssinaturaOriginal = assinatura,
        VigenciaFim = vigencia,
    };

    [Fact]
    public void Conta_cada_balde_do_painel()
    {
        var contratos = new[]
        {
            Contrato(Situacao.Assinado, new DateOnly(2026, 1, 1), new DateOnly(2027, 5, 1)),   // assinado, vigente
            Contrato(Situacao.Assinado, new DateOnly(2026, 1, 1), new DateOnly(2026, 9, 10)),  // assinado, a vencer
            Contrato(Situacao.Assinado, new DateOnly(2026, 1, 1), new DateOnly(2026, 7, 1)),   // assinado, vencido
            Contrato(Situacao.AguardandoAssinaturaProfissional, null, new DateOnly(2026, 12, 1)),
            Contrato(Situacao.Duvida, null, null),                                             // problema, sem data
            Contrato(Situacao.SemContrato, null, null),                                         // nem pendente nem problema
            Contrato(Situacao.AguardandoAssinaturaDigital, new DateOnly(2026, 2, 2), new DateOnly(2027, 1, 1)), // inconsistente
        };

        ResumoPainel resumo = CalculadoraResumo.Calcular(contratos, Hoje, diasAlerta: 30);

        Assert.Equal(7, resumo.Total);
        Assert.Equal(4, resumo.Assinados);              // os 3 primeiros + o inconsistente (tem data, sem aditivo pendente)
        Assert.Equal(1, resumo.PendentesDeAssinatura);
        Assert.Equal(1, resumo.Vencidos);
        Assert.Equal(1, resumo.AVencer);
        Assert.Equal(1, resumo.ComProblema);
        Assert.Equal(2, resumo.SemData);
        Assert.Equal(1, resumo.Inconsistentes);
    }

    [Fact]
    public void Agrupa_por_situacao_incluindo_zeros()
    {
        var contratos = new[] { Contrato(Situacao.Duvida, null, null) };

        ResumoPainel resumo = CalculadoraResumo.Calcular(contratos, Hoje, 30);

        Assert.Equal(1, resumo.PorSituacao[Situacao.Duvida]);
        Assert.Equal(0, resumo.PorSituacao[Situacao.Assinado]);
        Assert.Equal(Enum.GetValues<Situacao>().Length, resumo.PorSituacao.Count);
    }

    [Fact]
    public void Vencimentos_dos_proximos_meses_comecam_no_mes_de_hoje()
    {
        var contratos = new[]
        {
            Contrato(Situacao.Assinado, new DateOnly(2026, 1, 1), new DateOnly(2026, 8, 31)),
            Contrato(Situacao.Assinado, new DateOnly(2026, 1, 1), new DateOnly(2026, 10, 5)),
            Contrato(Situacao.Assinado, new DateOnly(2026, 1, 1), new DateOnly(2026, 10, 20)),
            Contrato(Situacao.Assinado, new DateOnly(2026, 1, 1), new DateOnly(2028, 1, 1)), // fora da janela
        };

        ResumoPainel resumo = CalculadoraResumo.Calcular(contratos, Hoje, 30, mesesAdiante: 6);

        Assert.Equal(6, resumo.ProximosMeses.Count);
        Assert.Equal((2026, 8), (resumo.ProximosMeses[0].Ano, resumo.ProximosMeses[0].Mes));
        Assert.Equal(1, resumo.ProximosMeses[0].Quantidade);
        Assert.Equal(0, resumo.ProximosMeses[1].Quantidade);              // setembro
        Assert.Equal(2, resumo.ProximosMeses[2].Quantidade);              // outubro
    }

    [Fact]
    public void Lista_vazia_da_resumo_zerado_sem_estourar()
    {
        ResumoPainel resumo = CalculadoraResumo.Calcular(Array.Empty<Contrato>(), Hoje, 30);
        Assert.Equal(0, resumo.Total);
        Assert.Equal(6, resumo.ProximosMeses.Count);
    }
}
