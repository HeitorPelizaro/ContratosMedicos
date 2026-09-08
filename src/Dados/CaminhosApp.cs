namespace ContratosMedicos.Dados;

/// <summary>
/// Onde os dados da usuária moram. No Windows: %LOCALAPPDATA%\ContratosMedicos.
/// A raiz é injetável para os testes não encostarem na pasta real.
/// </summary>
public class CaminhosApp
{
    public CaminhosApp(string? raiz = null)
    {
        PastaDados = raiz ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ContratosMedicos");
    }

    public string PastaDados { get; }
    public string ArquivoBanco => Path.Combine(PastaDados, "dados.db");
    public string PastaBackups => Path.Combine(PastaDados, "backups");
    public string PastaAnexos => Path.Combine(PastaDados, "anexos");
    public string PastaLog => Path.Combine(PastaDados, "log");
    public string StringDeConexao => $"Data Source={ArquivoBanco}";

    public void GarantirPastas()
    {
        Directory.CreateDirectory(PastaDados);
        Directory.CreateDirectory(PastaBackups);
        Directory.CreateDirectory(PastaAnexos);
        Directory.CreateDirectory(PastaLog);
    }
}
