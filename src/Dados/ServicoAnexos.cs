using System.Diagnostics;
using ContratosMedicos.Dominio;

namespace ContratosMedicos.Dados;

/// <summary>
/// Guarda os PDFs na pasta de anexos com nome neutro (GUID), preservando o nome
/// original só para exibir. Assim acento e caractere inválido no Windows não atrapalham.
/// </summary>
public class ServicoAnexos
{
    private readonly CaminhosApp _caminhos;

    public ServicoAnexos(CaminhosApp caminhos) => _caminhos = caminhos;

    public async Task<Anexo> SalvarAsync(Stream conteudo, string nomeArquivo)
    {
        Directory.CreateDirectory(_caminhos.PastaAnexos);

        string extensao = Path.GetExtension(nomeArquivo);
        string nomeNoDisco = $"{Guid.NewGuid():N}{extensao}";
        string destino = Path.Combine(_caminhos.PastaAnexos, nomeNoDisco);

        await using FileStream arquivo = File.Create(destino);
        await conteudo.CopyToAsync(arquivo);

        return new Anexo { NomeArquivo = nomeArquivo, CaminhoRelativo = nomeNoDisco };
    }

    public string CaminhoAbsoluto(Anexo anexo) =>
        Path.Combine(_caminhos.PastaAnexos, anexo.CaminhoRelativo);

    public void Excluir(Anexo anexo)
    {
        string caminho = CaminhoAbsoluto(anexo);
        if (File.Exists(caminho)) File.Delete(caminho);
    }

    /// <summary>Abre no leitor de PDF do Windows.</summary>
    public void AbrirNoSistema(Anexo anexo)
    {
        try
        {
            Process.Start(new ProcessStartInfo(CaminhoAbsoluto(anexo)) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Sem leitor associado: silencioso, o log do host registra.
        }
    }
}
