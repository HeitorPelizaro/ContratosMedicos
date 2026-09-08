namespace ContratosMedicos.App;

/// <summary>Estado vivo do processo: hoje (uma vez por render) e a URL com token.</summary>
public class EstadoDaSessao
{
    public string Token { get; } = Guid.NewGuid().ToString("N");
    public string UrlBase { get; set; } = "";
    public DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);
}
