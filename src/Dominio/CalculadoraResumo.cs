namespace ContratosMedicos.Dominio;

public static class CalculadoraResumo
{
    public static ResumoPainel Calcular(
        IEnumerable<Contrato> contratos,
        DateOnly hoje,
        int diasAlerta,
        int mesesAdiante = 6)
    {
        List<Contrato> lista = contratos.ToList();

        Dictionary<Situacao, int> porSituacao = Enum.GetValues<Situacao>()
            .ToDictionary(s => s, _ => 0);
        foreach (Contrato contrato in lista) porSituacao[contrato.Situacao]++;

        var janela = new List<VencimentosNoMes>();
        for (int i = 0; i < mesesAdiante; i++)
        {
            DateOnly mes = new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(i);
            int quantidade = lista.Count(c =>
            {
                DateOnly? vigencia = RegrasContrato.VigenciaEfetiva(c);
                return vigencia is not null
                       && vigencia.Value.Year == mes.Year
                       && vigencia.Value.Month == mes.Month;
            });
            janela.Add(new VencimentosNoMes(mes.Year, mes.Month, quantidade));
        }

        return new ResumoPainel(
            Total: lista.Count,
            Assinados: lista.Count(RegrasContrato.EstaAssinado),
            PendentesDeAssinatura: lista.Count(RegrasContrato.EstaPendenteDeAssinatura),
            Vencidos: lista.Count(c => RegrasContrato.ClassificarVigencia(c, hoje, diasAlerta) == ClassificacaoVigencia.Vencida),
            AVencer: lista.Count(c => RegrasContrato.ClassificarVigencia(c, hoje, diasAlerta) == ClassificacaoVigencia.AVencer),
            ComProblema: lista.Count(RegrasContrato.TemProblema),
            SemData: lista.Count(c => RegrasContrato.VigenciaEfetiva(c) is null),
            Inconsistentes: lista.Count(RegrasContrato.TemInconsistencia),
            PorSituacao: porSituacao,
            ProximosMeses: janela);
    }
}
