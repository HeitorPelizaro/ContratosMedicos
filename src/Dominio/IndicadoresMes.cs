using System.Globalization;

namespace ContratosMedicos.Dominio;

public record LinhaIndicador(
    string Rotulo,
    int Medicos,
    int Contratos,
    int ContratosAssinados,
    int MedicosComContratoAssinado);

/// <summary>
/// O indicador que vai para a gerência todo mês: quantidade de médicos x contratos assinados.
/// Traz a posição no fim do mês e o movimento do mês, mais a quebra por especialidade.
/// </summary>
public record IndicadoresMes(
    int Ano,
    int Mes,
    DateOnly Corte,
    int MedicosDistintos,
    int MedicosComContratoAssinado,
    int ContratosTotal,
    int ContratosAssinadosAte,
    int ContratosAssinadosNoMes,
    int AditivosAssinadosNoMes,
    int MedicosDistintosNoMes,
    IReadOnlyList<LinhaIndicador> PorEspecialidade)
{
    /// <summary>Nulo quando não há contrato nenhum — a tela mostra "—", nunca 0%.</summary>
    public decimal? PercentualCobertura => ContratosTotal == 0
        ? null
        : Math.Round(ContratosAssinadosAte * 100m / ContratosTotal, 1);

    public string NomeDoMes =>
        new DateOnly(Ano, Mes, 1).ToString("MMMM 'de' yyyy", CultureInfo.GetCultureInfo("pt-BR"));
}
