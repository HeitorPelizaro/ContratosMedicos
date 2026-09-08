namespace ContratosMedicos.Dominio;

public class Anexo
{
    public int Id { get; set; }
    public int? ContratoId { get; set; }
    public int? AditivoId { get; set; }
    public string NomeArquivo { get; set; } = "";
    /// <summary>Caminho relativo dentro da pasta anexos\ do app.</summary>
    public string CaminhoRelativo { get; set; } = "";
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}
