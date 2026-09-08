using ContratosMedicos.Dominio;

namespace ContratosMedicos.Dados;

public enum RecorteVigencia { Todos = 0, Vencida, AVencer, Vigente, SemData }

/// <summary>Os recortes que os cartões do painel abrem na lista.</summary>
public enum RecortePainel { Todos = 0, Assinados, Pendentes, Vencidos, AVencer, ComProblema, Inconsistentes }

public class FiltroContratos
{
    public string? Busca { get; set; }
    public Situacao? Situacao { get; set; }
    public string? Especialidade { get; set; }
    public string? Empresa { get; set; }
    public RecorteVigencia Vigencia { get; set; } = RecorteVigencia.Todos;
    public RecortePainel Recorte { get; set; } = RecortePainel.Todos;

    public bool AlgumFiltroAtivo =>
        !string.IsNullOrWhiteSpace(Busca) || Situacao is not null
        || Especialidade is not null || Empresa is not null
        || Vigencia != RecorteVigencia.Todos || Recorte != RecortePainel.Todos;
}
