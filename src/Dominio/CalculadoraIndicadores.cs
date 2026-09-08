using System.Globalization;
using System.Text;

namespace ContratosMedicos.Dominio;

public static class CalculadoraIndicadores
{
    private const string EspecialidadeVazia = "Não informada";

    public static IndicadoresMes Calcular(IEnumerable<Contrato> contratos, int ano, int mes)
    {
        List<Contrato> lista = contratos.ToList();
        var primeiroDia = new DateOnly(ano, mes, 1);
        var corte = new DateOnly(ano, mes, DateTime.DaysInMonth(ano, mes));

        List<Contrato> assinadosAte = lista.Where(c => AssinadoAte(c, corte)).ToList();
        List<Contrato> assinadosNoMes = lista
            .Where(c => c.DataAssinaturaOriginal is DateOnly data
                        && data >= primeiroDia && data <= corte)
            .ToList();

        int aditivosNoMes = lista.Sum(c => c.Aditivos.Count(a =>
            a.DataAssinatura is DateOnly data
            && a.Situacao == Situacao.Assinado
            && data >= primeiroDia && data <= corte));

        var porEspecialidade = lista
            .GroupBy(c => string.IsNullOrWhiteSpace(c.Especialidade)
                ? EspecialidadeVazia
                : c.Especialidade!.Trim())
            .Select(grupo => new LinhaIndicador(
                Rotulo: grupo.Key,
                Medicos: MedicosDistintos(grupo),
                Contratos: grupo.Count(),
                ContratosAssinados: grupo.Count(c => AssinadoAte(c, corte)),
                MedicosComContratoAssinado: MedicosDistintos(grupo.Where(c => AssinadoAte(c, corte)))))
            .OrderBy(l => l.Rotulo, StringComparer.CurrentCulture)
            .ToList();

        return new IndicadoresMes(
            Ano: ano,
            Mes: mes,
            Corte: corte,
            MedicosDistintos: MedicosDistintos(lista),
            MedicosComContratoAssinado: MedicosDistintos(assinadosAte),
            ContratosTotal: lista.Count,
            ContratosAssinadosAte: assinadosAte.Count,
            ContratosAssinadosNoMes: assinadosNoMes.Count,
            AditivosAssinadosNoMes: aditivosNoMes,
            MedicosDistintosNoMes: MedicosDistintos(assinadosNoMes),
            PorEspecialidade: porEspecialidade);
    }

    /// <summary>
    /// Posição histórica: só a data de assinatura importa. Aditivo pendente HOJE não
    /// desfaz o que já estava assinado no fim daquele mês.
    /// </summary>
    private static bool AssinadoAte(Contrato contrato, DateOnly corte) =>
        contrato.DataAssinaturaOriginal is DateOnly data && data <= corte;

    /// <summary>Um médico com vários contratos conta uma vez. Contrato sem profissional não conta.</summary>
    private static int MedicosDistintos(IEnumerable<Contrato> contratos) =>
        contratos
            .Select(c => ChaveDoProfissional(c.Profissional))
            .Where(chave => chave.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Count();

    /// <summary>Maiúsculas, sem acento, espaços colapsados — "Dra. Angélica" == "DRA. ANGELICA".</summary>
    private static string ChaveDoProfissional(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "";

        string decomposto = nome.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);
        foreach (char c in decomposto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                construtor.Append(char.ToUpperInvariant(c));

        return string.Join(' ', construtor.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
