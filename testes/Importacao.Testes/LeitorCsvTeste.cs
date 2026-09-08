using System.Text;

namespace ContratosMedicos.Importacao.Testes;

public class LeitorCsvTeste : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "cm-csv-" + Guid.NewGuid());

    public LeitorCsvTeste() => Directory.CreateDirectory(_pasta);

    private string Escrever(string nome, string conteudo, Encoding codificacao)
    {
        string caminho = Path.Combine(_pasta, nome);
        File.WriteAllText(caminho, conteudo, codificacao);
        return caminho;
    }

    [Fact]
    public void Le_csv_com_ponto_e_virgula()
    {
        string arquivo = Escrever("pv.csv",
            "PROFISSIONAL;ESPECIALIDADE;VIGÊNCIA\nDra. Angélica;Cardiologia;31/12/2026\n",
            Encoding.UTF8);

        PlanilhaLida lida = new LeitorCsv().Ler(arquivo);

        Assert.Equal(new[] { "PROFISSIONAL", "ESPECIALIDADE", "VIGÊNCIA" }, lida.Cabecalhos);
        Assert.Equal("Dra. Angélica", Assert.Single(lida.Linhas).Celulas[0]);
    }

    [Fact]
    public void Le_csv_com_virgula()
    {
        string arquivo = Escrever("v.csv",
            "PROFISSIONAL,ESPECIALIDADE\nDr. B,Ortopedia\n", Encoding.UTF8);

        PlanilhaLida lida = new LeitorCsv().Ler(arquivo);

        Assert.Equal(2, lida.Cabecalhos.Count);
        Assert.Equal("Ortopedia", Assert.Single(lida.Linhas).Celulas[1]);
    }

    [Fact]
    public void Le_csv_em_windows_1252_sem_estragar_acento()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string arquivo = Escrever("1252.csv",
            "PROFISSIONAL;ESPECIALIDADE\nDra. Angélica Formiga;Anestesiologia\n",
            Encoding.GetEncoding(1252));

        PlanilhaLida lida = new LeitorCsv().Ler(arquivo);

        Assert.Equal("Dra. Angélica Formiga", Assert.Single(lida.Linhas).Celulas[0]);
    }

    [Fact]
    public void Respeita_campo_entre_aspas_com_separador_dentro()
    {
        string arquivo = Escrever("aspas.csv",
            "PROFISSIONAL;OBS\nDr. C;\"cardiologia; hemodinâmica\"\n", Encoding.UTF8);

        Assert.Equal("cardiologia; hemodinâmica",
            Assert.Single(new LeitorCsv().Ler(arquivo).Linhas).Celulas[1]);
    }

    [Fact]
    public void Ignora_linha_em_branco_e_conserva_o_numero_da_linha()
    {
        string arquivo = Escrever("branco.csv",
            "PROFISSIONAL\nDr. A\n\nDr. B\n", Encoding.UTF8);

        PlanilhaLida lida = new LeitorCsv().Ler(arquivo);

        Assert.Equal(2, lida.Linhas.Count);
        Assert.Equal(4, lida.Linhas[1].Numero);
    }

    [Fact]
    public void Csv_nao_tem_cor_nenhuma()
    {
        string arquivo = Escrever("semcor.csv", "PROFISSIONAL\nDr. A\n", Encoding.UTF8);

        PlanilhaLida lida = new LeitorCsv().Ler(arquivo);

        Assert.Empty(lida.MapaCores);
        Assert.Null(lida.Linhas[0].ChaveCor);
    }

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }
}
