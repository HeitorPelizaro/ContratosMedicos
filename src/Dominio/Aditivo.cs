namespace ContratosMedicos.Dominio;

public class Aditivo
{
    public int Id { get; set; }
    public int ContratoId { get; set; }
    public TipoAditivo Tipo { get; set; } = TipoAditivo.Outro;
    public string? Numero { get; set; }
    public DateOnly? DataAssinatura { get; set; }
    public Situacao Situacao { get; set; } = Situacao.NaoClassificado;
    /// <summary>Só faz sentido em aditivo de vigência.</summary>
    public DateOnly? NovaVigenciaFim { get; set; }
    /// <summary>Só faz sentido em aditivo de valor.</summary>
    public decimal? Valor { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public List<Anexo> Anexos { get; set; } = new();
}
