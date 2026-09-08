namespace ContratosMedicos.Dominio.Testes;

public class CalculadoraIndicadoresTeste
{
    private static Contrato Contrato(
        string profissional,
        DateOnly? assinatura,
        string especialidade = "Cardiologia") => new()
    {
        Profissional = profissional,
        Especialidade = especialidade,
        DataAssinaturaOriginal = assinatura,
        Situacao = assinatura is null ? Situacao.AguardandoAssinaturaProfissional : Situacao.Assinado,
    };

    [Fact]
    public void Medico_com_tres_contratos_conta_uma_vez()
    {
        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(new[]
        {
            Contrato("Dra. Angélica", new DateOnly(2026, 3, 1)),
            Contrato("Dra. Angélica", new DateOnly(2026, 4, 1)),
            Contrato("Dra. Angélica", new DateOnly(2026, 5, 1)),
        }, 2026, 7);

        Assert.Equal(1, indicadores.MedicosDistintos);
        Assert.Equal(3, indicadores.ContratosTotal);
        Assert.Equal(3, indicadores.ContratosAssinadosAte);
        Assert.Equal(1, indicadores.MedicosComContratoAssinado);
    }

    [Fact]
    public void Mesmo_nome_com_acento_e_caixa_diferentes_conta_uma_vez()
    {
        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(new[]
        {
            Contrato("Dra. Angélica Formiga", new DateOnly(2026, 3, 1)),
            Contrato("DRA. ANGELICA FORMIGA", null),
            Contrato("  Dra.   Angélica   Formiga ", null),
        }, 2026, 7);

        Assert.Equal(1, indicadores.MedicosDistintos);
    }

    [Fact]
    public void Corte_e_o_ultimo_dia_do_mes_pedido()
    {
        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(Array.Empty<Contrato>(), 2026, 2);
        Assert.Equal(new DateOnly(2026, 2, 28), indicadores.Corte);
    }

    [Fact]
    public void Assinatura_depois_do_corte_nao_entra_na_posicao()
    {
        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(new[]
        {
            Contrato("Dr. A", new DateOnly(2026, 7, 31)),
            Contrato("Dr. B", new DateOnly(2026, 8, 1)),
        }, 2026, 7);

        Assert.Equal(2, indicadores.ContratosTotal);
        Assert.Equal(1, indicadores.ContratosAssinadosAte);
        Assert.Equal(1, indicadores.MedicosComContratoAssinado);
    }

    [Fact]
    public void Movimento_do_mes_pega_primeiro_e_ultimo_dia()
    {
        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(new[]
        {
            Contrato("Dr. A", new DateOnly(2026, 7, 1)),
            Contrato("Dr. B", new DateOnly(2026, 7, 31)),
            Contrato("Dr. C", new DateOnly(2026, 6, 30)),
            Contrato("Dr. D", new DateOnly(2026, 8, 1)),
        }, 2026, 7);

        Assert.Equal(2, indicadores.ContratosAssinadosNoMes);
        Assert.Equal(2, indicadores.MedicosDistintosNoMes);
    }

    [Fact]
    public void Aditivos_assinados_no_mes_sao_contados_separadamente()
    {
        var contrato = Contrato("Dr. A", new DateOnly(2026, 1, 10));
        contrato.Aditivos.Add(new Aditivo
        {
            Tipo = TipoAditivo.Valor, Situacao = Situacao.Assinado,
            DataAssinatura = new DateOnly(2026, 7, 15),
        });
        contrato.Aditivos.Add(new Aditivo
        {
            Tipo = TipoAditivo.Vigencia, Situacao = Situacao.TaVigenciaAguardandoAssinatura,
            DataAssinatura = null,
        });
        contrato.Aditivos.Add(new Aditivo
        {
            Tipo = TipoAditivo.Outro, Situacao = Situacao.Assinado,
            DataAssinatura = new DateOnly(2026, 6, 1),
        });

        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(new[] { contrato }, 2026, 7);

        Assert.Equal(1, indicadores.AditivosAssinadosNoMes);
    }

    [Fact]
    public void Percentual_de_cobertura_tem_uma_casa_decimal()
    {
        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(new[]
        {
            Contrato("Dr. A", new DateOnly(2026, 1, 1)),
            Contrato("Dr. B", new DateOnly(2026, 1, 1)),
            Contrato("Dr. C", null),
        }, 2026, 7);

        Assert.Equal(66.7m, indicadores.PercentualCobertura);
    }

    [Fact]
    public void Sem_contrato_nenhum_o_percentual_e_nulo_e_nada_estoura()
    {
        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(Array.Empty<Contrato>(), 2026, 7);

        Assert.Equal(0, indicadores.ContratosTotal);
        Assert.Null(indicadores.PercentualCobertura);
        Assert.Empty(indicadores.PorEspecialidade);
    }

    [Fact]
    public void Quebra_por_especialidade_vem_ordenada_e_com_os_mesmos_criterios()
    {
        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(new[]
        {
            Contrato("Dr. A", new DateOnly(2026, 1, 1), "Ortopedia"),
            Contrato("Dr. B", null, "Cardiologia"),
            Contrato("Dr. C", new DateOnly(2026, 1, 1), "Cardiologia"),
            Contrato("Dr. C", new DateOnly(2026, 2, 1), "Cardiologia"),
        }, 2026, 7);

        Assert.Equal(new[] { "Cardiologia", "Ortopedia" },
            indicadores.PorEspecialidade.Select(l => l.Rotulo).ToArray());

        LinhaIndicador cardiologia = indicadores.PorEspecialidade[0];
        Assert.Equal(2, cardiologia.Medicos);
        Assert.Equal(3, cardiologia.Contratos);
        Assert.Equal(2, cardiologia.ContratosAssinados);
        Assert.Equal(1, cardiologia.MedicosComContratoAssinado);
    }

    [Fact]
    public void Contrato_sem_especialidade_entra_como_nao_informada()
    {
        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(new[]
        {
            Contrato("Dr. A", new DateOnly(2026, 1, 1), especialidade: ""),
        }, 2026, 7);

        Assert.Equal("Não informada", Assert.Single(indicadores.PorEspecialidade).Rotulo);
    }

    [Fact]
    public void Contrato_sem_profissional_conta_no_total_mas_nao_como_medico()
    {
        IndicadoresMes indicadores = CalculadoraIndicadores.Calcular(new[]
        {
            new Contrato { Profissional = null, DataAssinaturaOriginal = new DateOnly(2026, 1, 1) },
        }, 2026, 7);

        Assert.Equal(1, indicadores.ContratosTotal);
        Assert.Equal(1, indicadores.ContratosAssinadosAte);
        Assert.Equal(0, indicadores.MedicosDistintos);
    }

    [Fact]
    public void Nome_do_mes_sai_em_portugues()
    {
        Assert.Equal("julho de 2026",
            CalculadoraIndicadores.Calcular(Array.Empty<Contrato>(), 2026, 7).NomeDoMes);
    }
}
