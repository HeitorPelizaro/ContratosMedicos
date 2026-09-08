namespace ContratosMedicos.Dominio.Testes;

public class RegrasContratoTeste
{
    private static readonly DateOnly Hoje = new(2026, 8, 31);

    private static Contrato ContratoAssinado() => new()
    {
        Profissional = "Angélica Formiga",
        Situacao = Situacao.Assinado,
        DataAssinaturaOriginal = new DateOnly(2026, 1, 10),
        VigenciaFim = new DateOnly(2026, 12, 31),
    };

    [Fact]
    public void Aditivo_sem_data_de_assinatura_esta_pendente()
    {
        Assert.True(RegrasContrato.AditivoEstaPendente(
            new Aditivo { Situacao = Situacao.Assinado, DataAssinatura = null }));
    }

    [Fact]
    public void Aditivo_com_situacao_aguardando_esta_pendente_mesmo_com_data()
    {
        Assert.True(RegrasContrato.AditivoEstaPendente(new Aditivo
        {
            Situacao = Situacao.AguardandoAssinaturaDigital,
            DataAssinatura = new DateOnly(2026, 7, 1),
        }));
    }

    [Fact]
    public void Aditivo_assinado_com_data_nao_esta_pendente()
    {
        Assert.False(RegrasContrato.AditivoEstaPendente(new Aditivo
        {
            Situacao = Situacao.Assinado,
            DataAssinatura = new DateOnly(2026, 7, 1),
        }));
    }

    [Fact]
    public void Contrato_com_data_de_assinatura_e_sem_aditivo_pendente_esta_assinado()
    {
        Assert.True(RegrasContrato.EstaAssinado(ContratoAssinado()));
    }

    [Fact]
    public void Contrato_sem_data_de_assinatura_nao_esta_assinado()
    {
        var contrato = ContratoAssinado();
        contrato.DataAssinaturaOriginal = null;
        Assert.False(RegrasContrato.EstaAssinado(contrato));
    }

    [Fact]
    public void Contrato_com_aditivo_pendente_nao_esta_assinado()
    {
        var contrato = ContratoAssinado();
        contrato.Aditivos.Add(new Aditivo
        {
            Tipo = TipoAditivo.Valor,
            Situacao = Situacao.AguardandoAssinaturaProfissional,
        });
        Assert.False(RegrasContrato.EstaAssinado(contrato));
    }

    [Fact]
    public void Data_de_assinatura_com_situacao_aguardando_e_inconsistencia()
    {
        var contrato = ContratoAssinado();
        contrato.Situacao = Situacao.AguardandoAssinaturaInstitucional;
        Assert.True(RegrasContrato.TemInconsistencia(contrato));
    }

    [Fact]
    public void Contrato_coerente_nao_tem_inconsistencia()
    {
        Assert.False(RegrasContrato.TemInconsistencia(ContratoAssinado()));
    }

    [Fact]
    public void Problema_e_duvida_contam_como_problema()
    {
        Assert.True(RegrasContrato.TemProblema(new Contrato { Situacao = Situacao.Duvida }));
        Assert.True(RegrasContrato.TemProblema(new Contrato { Situacao = Situacao.ProblemaNoContratoOuAditivo }));
        Assert.False(RegrasContrato.TemProblema(ContratoAssinado()));
    }

    [Fact]
    public void Pendente_de_assinatura_exclui_assinado_problema_e_sem_contrato()
    {
        Assert.False(RegrasContrato.EstaPendenteDeAssinatura(ContratoAssinado()));
        Assert.False(RegrasContrato.EstaPendenteDeAssinatura(new Contrato { Situacao = Situacao.Duvida }));
        Assert.False(RegrasContrato.EstaPendenteDeAssinatura(new Contrato { Situacao = Situacao.SemContrato }));
        Assert.True(RegrasContrato.EstaPendenteDeAssinatura(
            new Contrato { Situacao = Situacao.AguardandoAssinaturaProfissional }));
    }

    [Fact]
    public void Vigencia_efetiva_sem_aditivo_e_a_do_contrato()
    {
        Assert.Equal(new DateOnly(2026, 12, 31), RegrasContrato.VigenciaEfetiva(ContratoAssinado()));
    }

    [Fact]
    public void Aditivo_de_vigencia_assinado_prorroga_a_vigencia()
    {
        var contrato = ContratoAssinado();
        contrato.Aditivos.Add(new Aditivo
        {
            Tipo = TipoAditivo.Vigencia,
            Situacao = Situacao.Assinado,
            DataAssinatura = new DateOnly(2026, 6, 1),
            NovaVigenciaFim = new DateOnly(2027, 12, 31),
        });
        Assert.Equal(new DateOnly(2027, 12, 31), RegrasContrato.VigenciaEfetiva(contrato));
    }

    [Fact]
    public void Aditivo_de_vigencia_pendente_nao_prorroga()
    {
        var contrato = ContratoAssinado();
        contrato.Aditivos.Add(new Aditivo
        {
            Tipo = TipoAditivo.Vigencia,
            Situacao = Situacao.TaVigenciaAguardandoAssinatura,
            NovaVigenciaFim = new DateOnly(2027, 12, 31),
        });
        Assert.Equal(new DateOnly(2026, 12, 31), RegrasContrato.VigenciaEfetiva(contrato));
    }

    [Fact]
    public void Aditivo_de_valor_nao_mexe_na_vigencia()
    {
        var contrato = ContratoAssinado();
        contrato.Aditivos.Add(new Aditivo
        {
            Tipo = TipoAditivo.Valor,
            Situacao = Situacao.Assinado,
            DataAssinatura = new DateOnly(2026, 7, 15),
            Valor = 12500.50m,
        });
        Assert.Equal(new DateOnly(2026, 12, 31), RegrasContrato.VigenciaEfetiva(contrato));
    }

    [Fact]
    public void Entre_dois_aditivos_de_vigencia_vale_o_assinado_mais_recente()
    {
        var contrato = ContratoAssinado();
        contrato.Aditivos.Add(new Aditivo
        {
            Tipo = TipoAditivo.Vigencia, Situacao = Situacao.Assinado,
            DataAssinatura = new DateOnly(2025, 6, 1), NovaVigenciaFim = new DateOnly(2027, 6, 30),
        });
        contrato.Aditivos.Add(new Aditivo
        {
            Tipo = TipoAditivo.Vigencia, Situacao = Situacao.Assinado,
            DataAssinatura = new DateOnly(2026, 6, 1), NovaVigenciaFim = new DateOnly(2027, 12, 31),
        });
        Assert.Equal(new DateOnly(2027, 12, 31), RegrasContrato.VigenciaEfetiva(contrato));
    }

    [Fact]
    public void Sem_data_de_vigencia_a_classificacao_e_sem_data()
    {
        var contrato = ContratoAssinado();
        contrato.VigenciaFim = null;
        Assert.Equal(ClassificacaoVigencia.SemData,
            RegrasContrato.ClassificarVigencia(contrato, Hoje, 30));
        Assert.Null(RegrasContrato.DiasParaVencer(contrato, Hoje));
    }

    [Theory]
    [InlineData(2026, 8, 30, ClassificacaoVigencia.Vencida)]   // ontem
    [InlineData(2026, 8, 31, ClassificacaoVigencia.AVencer)]   // hoje ainda vale
    [InlineData(2026, 9, 1, ClassificacaoVigencia.AVencer)]    // amanhã
    [InlineData(2026, 9, 30, ClassificacaoVigencia.AVencer)]   // exatamente 30 dias
    [InlineData(2026, 10, 1, ClassificacaoVigencia.Vigente)]   // 31 dias
    public void Limites_do_alerta_de_30_dias(int ano, int mes, int dia, ClassificacaoVigencia esperada)
    {
        var contrato = ContratoAssinado();
        contrato.VigenciaFim = new DateOnly(ano, mes, dia);
        Assert.Equal(esperada, RegrasContrato.ClassificarVigencia(contrato, Hoje, 30));
    }

    [Fact]
    public void Dias_para_vencer_conta_do_hoje_ate_a_vigencia_efetiva()
    {
        var contrato = ContratoAssinado();
        contrato.VigenciaFim = new DateOnly(2026, 9, 10);
        Assert.Equal(10, RegrasContrato.DiasParaVencer(contrato, Hoje));
        contrato.VigenciaFim = new DateOnly(2026, 8, 21);
        Assert.Equal(-10, RegrasContrato.DiasParaVencer(contrato, Hoje));
    }

    [Fact]
    public void Prazo_de_alerta_e_configuravel()
    {
        var contrato = ContratoAssinado();
        contrato.VigenciaFim = new DateOnly(2026, 11, 15);
        Assert.Equal(ClassificacaoVigencia.Vigente, RegrasContrato.ClassificarVigencia(contrato, Hoje, 30));
        Assert.Equal(ClassificacaoVigencia.AVencer, RegrasContrato.ClassificarVigencia(contrato, Hoje, 90));
    }
}
