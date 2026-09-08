namespace ContratosMedicos.Dominio;

/// <summary>
/// Rótulo e cor de cada situação. As cores partem da legenda da planilha e são usadas
/// APENAS como marcador (tarja, ponto), nunca como fundo de área grande.
/// </summary>
public static class DescricoesSituacao
{
    private static readonly Dictionary<Situacao, (string Rotulo, string Cor)> Mapa = new()
    {
        [Situacao.Assinado] = ("Assinado", "#1B7F4F"),
        [Situacao.SemContrato] = ("Sem contrato", "#8A8F98"),
        [Situacao.PrazoAtrasadoSemProrrogacao] = ("Prazo atrasado, sem prorrogação confeccionada", "#B3261E"),
        [Situacao.AguardandoAssinaturaInstitucional] = ("Aguardando assinatura institucional", "#2A6F97"),
        [Situacao.ProblemaNoContratoOuAditivo] = ("Com problema no contrato/aditivo", "#C2410C"),
        [Situacao.Duvida] = ("Dúvida", "#7C3AED"),
        [Situacao.AguardandoAssinaturaProfissional] = ("Aguardando assinatura do profissional", "#B38600"),
        [Situacao.TaVigenciaAguardandoAssinatura] = ("T.A. de vigência feito, aguardando assinatura", "#0F766E"),
        [Situacao.AguardandoAssinaturaDigital] = ("Aguardando assinatura digital", "#4F46E5"),
        [Situacao.ApenasUmTermoAditivoEnviado] = ("Apenas um dos termos aditivos enviado", "#9D174D"),
        [Situacao.NaoClassificado] = ("Não classificado", "#5B6169"),
    };

    public static IReadOnlyList<Situacao> Todas { get; } = new[]
    {
        Situacao.Assinado,
        Situacao.AguardandoAssinaturaProfissional,
        Situacao.AguardandoAssinaturaInstitucional,
        Situacao.AguardandoAssinaturaDigital,
        Situacao.TaVigenciaAguardandoAssinatura,
        Situacao.ApenasUmTermoAditivoEnviado,
        Situacao.PrazoAtrasadoSemProrrogacao,
        Situacao.ProblemaNoContratoOuAditivo,
        Situacao.Duvida,
        Situacao.SemContrato,
        Situacao.NaoClassificado,
    };

    public static string Rotulo(Situacao situacao) => Mapa[situacao].Rotulo;

    public static string Cor(Situacao situacao) => Mapa[situacao].Cor;
}
