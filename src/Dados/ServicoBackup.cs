using System.Text.RegularExpressions;

namespace ContratosMedicos.Dados;

/// <summary>
/// Cópias do banco na pasta backups\. São dois produtores gravando ali, e cada um tem a
/// sua própria rotação — juntos, no mesmo bolo, meia dúzia de importações num dia só
/// apagariam o histórico de todos os dias anteriores:
///
///   • diária        dados-AAAA-MM-DD.db                              → guarda as 7 últimas
///   • importação    dados-AAAA-MM-DD-HHmmss-antes-da-importacao.db   → guarda as 7 últimas
///
/// Ou seja: 7 dias de histórico diário continuam de pé por mais importações que se faça,
/// e as 7 últimas importações também.
/// </summary>
public class ServicoBackup
{
    private const int DiariosMantidos = 7;
    private const int AvulsosMantidos = 7;

    /// <summary>Só a cópia diária: "dados-" + data + ".db", sem nada no meio.</summary>
    private static readonly Regex Diario =
        new(@"^dados-\d{4}-\d{2}-\d{2}\.db$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly CaminhosApp _caminhos;

    public ServicoBackup(CaminhosApp caminhos) => _caminhos = caminhos;

    /// <returns>Caminho do backup criado, ou null se já existe backup do dia.</returns>
    public string? CriarBackupSeNecessario(DateOnly hoje)
    {
        if (!File.Exists(_caminhos.ArquivoBanco)) return null;

        string destino = Path.Combine(_caminhos.PastaBackups, $"dados-{hoje:yyyy-MM-dd}.db");
        if (File.Exists(destino)) return null;

        Directory.CreateDirectory(_caminhos.PastaBackups);
        File.Copy(_caminhos.ArquivoBanco, destino);
        RemoverAntigos();
        return destino;
    }

    public string CriarBackupAgora(string sufixo)
    {
        Directory.CreateDirectory(_caminhos.PastaBackups);
        string destino = Path.Combine(
            _caminhos.PastaBackups,
            $"dados-{DateTime.Now:yyyy-MM-dd-HHmmss}-{sufixo}.db");
        File.Copy(_caminhos.ArquivoBanco, destino, overwrite: true);
        RemoverAntigos();
        return destino;
    }

    private void RemoverAntigos()
    {
        string[] arquivos = Directory.GetFiles(_caminhos.PastaBackups, "dados-*.db");

        Rotacionar(arquivos.Where(EhDiario), DiariosMantidos);
        Rotacionar(arquivos.Where(f => !EhDiario(f)), AvulsosMantidos);
    }

    private static bool EhDiario(string caminho) => Diario.IsMatch(Path.GetFileName(caminho));

    /// <summary>
    /// O nome começa pela data em AAAA-MM-DD[-HHmmss], então ordem alfabética é ordem
    /// cronológica — não dependemos da data do sistema de arquivos.
    /// </summary>
    private static void Rotacionar(IEnumerable<string> arquivos, int mantidos)
    {
        foreach (string velho in arquivos
            .OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal)
            .Skip(mantidos))
            File.Delete(velho);
    }
}
