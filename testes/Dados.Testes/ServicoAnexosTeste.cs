using System.Text;
using ContratosMedicos.Dominio;

namespace ContratosMedicos.Dados.Testes;

public class ServicoAnexosTeste : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "cm-anexos-" + Guid.NewGuid());
    private readonly ServicoAnexos _servico;
    private readonly CaminhosApp _caminhos;

    public ServicoAnexosTeste()
    {
        _caminhos = new CaminhosApp(_pasta);
        _caminhos.GarantirPastas();
        _servico = new ServicoAnexos(_caminhos);
    }

    [Fact]
    public async Task Salva_o_pdf_na_pasta_de_anexos_com_nome_unico()
    {
        using var conteudo = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 fake"));

        Anexo anexo = await _servico.SalvarAsync(conteudo, "Contrato Dra. Angélica.pdf");

        Assert.Equal("Contrato Dra. Angélica.pdf", anexo.NomeArquivo);
        Assert.True(File.Exists(_servico.CaminhoAbsoluto(anexo)));
        Assert.EndsWith(".pdf", anexo.CaminhoRelativo);
        Assert.DoesNotContain("Angélica", anexo.CaminhoRelativo); // nome no disco é neutro
    }

    [Fact]
    public async Task Dois_arquivos_de_mesmo_nome_nao_se_sobrescrevem()
    {
        using var a = new MemoryStream(Encoding.UTF8.GetBytes("a"));
        using var b = new MemoryStream(Encoding.UTF8.GetBytes("b"));

        Anexo primeiro = await _servico.SalvarAsync(a, "contrato.pdf");
        Anexo segundo = await _servico.SalvarAsync(b, "contrato.pdf");

        Assert.NotEqual(primeiro.CaminhoRelativo, segundo.CaminhoRelativo);
        Assert.Equal(2, Directory.GetFiles(_caminhos.PastaAnexos).Length);
    }

    [Fact]
    public async Task Excluir_apaga_o_arquivo_do_disco()
    {
        using var conteudo = new MemoryStream(Encoding.UTF8.GetBytes("x"));
        Anexo anexo = await _servico.SalvarAsync(conteudo, "some.pdf");

        _servico.Excluir(anexo);

        Assert.False(File.Exists(_servico.CaminhoAbsoluto(anexo)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }
}
