namespace ContratosMedicos.Dominio;

public class Contrato
{
    public int Id { get; set; }
    public string? CadastroRegrasCriterios { get; set; }
    public string? Cnpj { get; set; }
    public bool? OptanteSimplesNacional { get; set; }
    public string? Empresa { get; set; }
    public string? Profissional { get; set; }
    public string? Especialidade { get; set; }
    public string? AreaAtuacao { get; set; }
    public string? Edital { get; set; }
    public string? CodigoContratoTasy { get; set; }
    public string? ContratoOriginal { get; set; }
    public DateOnly? DataAssinaturaOriginal { get; set; }
    public DateOnly? VigenciaFim { get; set; }
    public string? Observacao { get; set; }
    public Situacao Situacao { get; set; } = Situacao.NaoClassificado;
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime AlteradoEm { get; set; } = DateTime.Now;
    public List<Aditivo> Aditivos { get; set; } = new();
    public List<Anexo> Anexos { get; set; } = new();
}
