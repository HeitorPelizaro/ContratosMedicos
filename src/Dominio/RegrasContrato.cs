namespace ContratosMedicos.Dominio;

/// <summary>
/// Regras derivadas do contrato. Puras e sem estado: "hoje" entra por parâmetro,
/// para os testes serem determinísticos e o painel poder simular datas.
/// </summary>
public static class RegrasContrato
{
    public static bool AditivoEstaPendente(Aditivo aditivo) =>
        aditivo.DataAssinatura is null || aditivo.Situacao != Situacao.Assinado;

    public static bool EstaAssinado(Contrato contrato) =>
        contrato.DataAssinaturaOriginal is not null
        && !contrato.Aditivos.Any(AditivoEstaPendente);

    public static bool TemInconsistencia(Contrato contrato) =>
        contrato.DataAssinaturaOriginal is not null && SituacaoDeEspera(contrato.Situacao);

    public static bool TemProblema(Contrato contrato) =>
        contrato.Situacao is Situacao.ProblemaNoContratoOuAditivo or Situacao.Duvida;

    public static bool EstaPendenteDeAssinatura(Contrato contrato) =>
        !EstaAssinado(contrato)
        && !TemProblema(contrato)
        && contrato.Situacao != Situacao.SemContrato;

    public static DateOnly? VigenciaEfetiva(Contrato contrato)
    {
        Aditivo? prorrogacao = contrato.Aditivos
            .Where(a => a.Tipo == TipoAditivo.Vigencia
                        && a.NovaVigenciaFim is not null
                        && !AditivoEstaPendente(a))
            .OrderBy(a => a.DataAssinatura)
            .ThenBy(a => a.NovaVigenciaFim)
            .LastOrDefault();

        return prorrogacao?.NovaVigenciaFim ?? contrato.VigenciaFim;
    }

    public static int? DiasParaVencer(Contrato contrato, DateOnly hoje)
    {
        DateOnly? vigencia = VigenciaEfetiva(contrato);
        return vigencia is null ? null : vigencia.Value.DayNumber - hoje.DayNumber;
    }

    public static ClassificacaoVigencia ClassificarVigencia(Contrato contrato, DateOnly hoje, int diasAlerta)
    {
        int? dias = DiasParaVencer(contrato, hoje);
        if (dias is null) return ClassificacaoVigencia.SemData;
        if (dias < 0) return ClassificacaoVigencia.Vencida;
        return dias <= diasAlerta ? ClassificacaoVigencia.AVencer : ClassificacaoVigencia.Vigente;
    }

    private static bool SituacaoDeEspera(Situacao situacao) =>
        situacao is Situacao.AguardandoAssinaturaProfissional
                 or Situacao.AguardandoAssinaturaInstitucional
                 or Situacao.AguardandoAssinaturaDigital
                 or Situacao.TaVigenciaAguardandoAssinatura
                 or Situacao.ApenasUmTermoAditivoEnviado;
}
