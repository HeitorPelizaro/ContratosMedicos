namespace ContratosMedicos.Dominio;

/// <summary>As 9 situações da aba LEGENDA CORES da planilha, mais Assinado e NaoClassificado.</summary>
public enum Situacao
{
    NaoClassificado = 0,
    Assinado = 1,
    SemContrato = 2,
    PrazoAtrasadoSemProrrogacao = 3,
    AguardandoAssinaturaInstitucional = 4,
    ProblemaNoContratoOuAditivo = 5,
    Duvida = 6,
    AguardandoAssinaturaProfissional = 7,
    TaVigenciaAguardandoAssinatura = 8,
    AguardandoAssinaturaDigital = 9,
    ApenasUmTermoAditivoEnviado = 10,
}
