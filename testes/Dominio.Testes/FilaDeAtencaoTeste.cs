namespace ContratosMedicos.Dominio.Testes;

public class FilaDeAtencaoTeste
{
    private static readonly DateOnly Hoje = new(2026, 8, 31);

    [Fact]
    public void Ordena_vencido_antes_de_a_vencer_antes_de_aditivo_antes_de_inconsistencia()
    {
        var vencido = new Contrato { Profissional = "Vencido", Situacao = Situacao.Assinado,
            DataAssinaturaOriginal = new DateOnly(2026, 1, 1), VigenciaFim = new DateOnly(2026, 1, 30) };
        var aVencer = new Contrato { Profissional = "A vencer", Situacao = Situacao.Assinado,
            DataAssinaturaOriginal = new DateOnly(2026, 1, 1), VigenciaFim = new DateOnly(2026, 9, 5) };
        var comAditivo = new Contrato { Profissional = "Com aditivo", Situacao = Situacao.Assinado,
            DataAssinaturaOriginal = new DateOnly(2026, 1, 1), VigenciaFim = new DateOnly(2027, 1, 1),
            Aditivos = { new Aditivo { Tipo = TipoAditivo.Valor, Situacao = Situacao.AguardandoAssinaturaDigital } } };
        var inconsistente = new Contrato { Profissional = "Inconsistente",
            Situacao = Situacao.AguardandoAssinaturaDigital,
            DataAssinaturaOriginal = new DateOnly(2026, 2, 2), VigenciaFim = new DateOnly(2027, 1, 1) };

        List<ItemAtencao> fila = FilaDeAtencao.Montar(
            new[] { inconsistente, comAditivo, aVencer, vencido }, Hoje, 30);

        Assert.Equal(new[] { MotivoAtencao.Vencido, MotivoAtencao.VenceEmBreve,
                             MotivoAtencao.AditivoPendente, MotivoAtencao.Inconsistencia },
            fila.Select(i => i.Motivo).ToArray());
    }

    [Fact]
    public void Vencidos_mais_antigos_vem_primeiro()
    {
        var antigo = new Contrato { Profissional = "Antigo", Situacao = Situacao.Assinado,
            DataAssinaturaOriginal = new DateOnly(2025, 1, 1), VigenciaFim = new DateOnly(2025, 1, 30) };
        var recente = new Contrato { Profissional = "Recente", Situacao = Situacao.Assinado,
            DataAssinaturaOriginal = new DateOnly(2026, 1, 1), VigenciaFim = new DateOnly(2026, 8, 20) };

        List<ItemAtencao> fila = FilaDeAtencao.Montar(new[] { recente, antigo }, Hoje, 30);

        Assert.Equal("Antigo", fila[0].Contrato.Profissional);
    }

    [Fact]
    public void Contrato_em_dia_nao_entra_na_fila()
    {
        var emDia = new Contrato { Situacao = Situacao.Assinado,
            DataAssinaturaOriginal = new DateOnly(2026, 1, 1), VigenciaFim = new DateOnly(2028, 1, 1) };

        Assert.Empty(FilaDeAtencao.Montar(new[] { emDia }, Hoje, 30));
    }

    [Fact]
    public void Explicacao_e_texto_em_portugues_com_o_numero_de_dias()
    {
        var vencido = new Contrato { Situacao = Situacao.Assinado,
            DataAssinaturaOriginal = new DateOnly(2026, 1, 1), VigenciaFim = new DateOnly(2026, 8, 21) };

        ItemAtencao item = Assert.Single(FilaDeAtencao.Montar(new[] { vencido }, Hoje, 30));

        Assert.Contains("10", item.Explicacao);
        Assert.Contains("venc", item.Explicacao, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Contrato_sem_vigencia_entra_por_ultimo()
    {
        var semData = new Contrato { Situacao = Situacao.Assinado,
            DataAssinaturaOriginal = new DateOnly(2026, 1, 1), VigenciaFim = null };

        Assert.Equal(MotivoAtencao.SemVigencia,
            Assert.Single(FilaDeAtencao.Montar(new[] { semData }, Hoje, 30)).Motivo);
    }

    [Fact]
    public void Um_contrato_com_dois_problemas_aparece_pelo_mais_urgente()
    {
        var vencidoEComAditivo = new Contrato { Profissional = "Dois problemas", Situacao = Situacao.Assinado,
            DataAssinaturaOriginal = new DateOnly(2026, 1, 1), VigenciaFim = new DateOnly(2026, 3, 1),
            Aditivos = { new Aditivo { Tipo = TipoAditivo.Valor, Situacao = Situacao.Duvida } } };

        ItemAtencao item = Assert.Single(FilaDeAtencao.Montar(new[] { vencidoEComAditivo }, Hoje, 30));
        Assert.Equal(MotivoAtencao.Vencido, item.Motivo);
    }
}
