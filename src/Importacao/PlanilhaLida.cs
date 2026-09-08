using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao;

/// <param name="Numero">Número real da linha na planilha, para os avisos citarem "linha 47".</param>
/// <param name="ChaveCor">Identificação da cor de fundo, ou null quando não há cor.</param>
public record LinhaPlanilha(int Numero, IReadOnlyList<string> Celulas, string? ChaveCor);

public record PlanilhaLida(
    IReadOnlyList<string> Cabecalhos,
    IReadOnlyList<LinhaPlanilha> Linhas,
    IReadOnlyDictionary<string, Situacao> MapaCores,
    IReadOnlyList<string> CoresSemLegenda);
