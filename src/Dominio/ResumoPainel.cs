namespace ContratosMedicos.Dominio;

public record VencimentosNoMes(int Ano, int Mes, int Quantidade);

public record ResumoPainel(
    int Total,
    int Assinados,
    int PendentesDeAssinatura,
    int Vencidos,
    int AVencer,
    int ComProblema,
    int SemData,
    int Inconsistentes,
    IReadOnlyDictionary<Situacao, int> PorSituacao,
    IReadOnlyList<VencimentosNoMes> ProximosMeses);
