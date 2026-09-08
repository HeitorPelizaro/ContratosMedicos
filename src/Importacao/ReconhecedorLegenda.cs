using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao;

/// <summary>
/// Traduz o texto escrito na aba LEGENDA CORES (ou numa coluna de situação) para o enum.
/// A ordem importa: os padrões mais específicos vêm primeiro.
/// </summary>
public static class ReconhecedorLegenda
{
    private static readonly (string Padrao, Situacao Situacao)[] Padroes =
    {
        ("APENAS UM", Situacao.ApenasUmTermoAditivoEnviado),
        ("ASSINATURA DIGITAL", Situacao.AguardandoAssinaturaDigital),
        ("ASSINATURA INSTITUCIONAL", Situacao.AguardandoAssinaturaInstitucional),
        ("ASSINATURA DO PROFISSIONAL", Situacao.AguardandoAssinaturaProfissional),
        ("VIGENCIA FEITO", Situacao.TaVigenciaAguardandoAssinatura),
        ("PRAZO ATRASADO", Situacao.PrazoAtrasadoSemProrrogacao),
        ("PROBLEMA", Situacao.ProblemaNoContratoOuAditivo),
        ("DUVIDA", Situacao.Duvida),
        ("SEM CONTRATO", Situacao.SemContrato),
        ("ASSINADO", Situacao.Assinado),
    };

    public static Situacao? Reconhecer(string? textoDaLegenda)
    {
        string normalizado = Texto.Normalizar(textoDaLegenda);
        if (normalizado.Length == 0) return null;

        foreach ((string padrao, Situacao situacao) in Padroes)
            if (normalizado.Contains(padrao, StringComparison.Ordinal))
                return situacao;

        return null;
    }
}
