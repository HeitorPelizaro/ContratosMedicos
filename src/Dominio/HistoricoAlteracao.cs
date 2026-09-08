namespace ContratosMedicos.Dominio;

public class HistoricoAlteracao
{
    public int Id { get; set; }
    public int ContratoId { get; set; }
    public DateTime QuandoEm { get; set; } = DateTime.Now;
    /// <summary>Texto em português, legível pela usuária. Ex.: "Vigência alterada de 31/12/2025 para 31/12/2026".</summary>
    public string Descricao { get; set; } = "";
}
