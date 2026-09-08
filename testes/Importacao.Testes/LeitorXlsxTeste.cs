using ClosedXML.Excel;
using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao.Testes;

public class LeitorXlsxTeste : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "cm-xlsx-" + Guid.NewGuid());

    public LeitorXlsxTeste() => Directory.CreateDirectory(_pasta);

    private string Caminho(string nome) => Path.Combine(_pasta, nome);

    [Fact]
    public void Le_cabecalhos_e_linhas_da_aba_geral()
    {
        string arquivo = Caminho("basica.xlsx");
        FabricaDePlanilhas.CriarXlsx(arquivo, new[]
        {
            (new[] { "Regras", "12.345.678/0001-99", "SIM", "Clínica X", "Dra. Angélica",
                     "Cardiologia", "Ambulatório", "Edital 1", "TASY-001", "Contrato 1",
                     "10/01/2026", "", "31/12/2026" }, (XLColor?)null),
        });

        PlanilhaLida lida = new LeitorXlsx().Ler(arquivo);

        Assert.Equal("CNPJ", lida.Cabecalhos[1]);
        Assert.Equal(13, lida.Cabecalhos.Count);
        LinhaPlanilha linha = Assert.Single(lida.Linhas);
        Assert.Equal(2, linha.Numero);
        Assert.Equal("Dra. Angélica", linha.Celulas[4]);
        Assert.Equal("31/12/2026", linha.Celulas[12]);
    }

    [Fact]
    public void Monta_o_mapa_de_cores_a_partir_da_aba_de_legenda()
    {
        string arquivo = Caminho("legenda.xlsx");
        FabricaDePlanilhas.CriarXlsx(arquivo, Array.Empty<(string[], XLColor?)>());

        PlanilhaLida lida = new LeitorXlsx().Ler(arquivo);

        Assert.Equal(FabricaDePlanilhas.LegendaPadrao.Length, lida.MapaCores.Count);
        Assert.Contains(Situacao.AguardandoAssinaturaProfissional, lida.MapaCores.Values);
    }

    [Fact]
    public void Linha_colorida_carrega_a_chave_de_cor_correspondente_a_legenda()
    {
        string arquivo = Caminho("colorida.xlsx");
        XLColor amarelo = FabricaDePlanilhas.LegendaPadrao[5].Cor; // aguardando profissional
        FabricaDePlanilhas.CriarXlsx(arquivo, new[]
        {
            (new[] { "", "", "", "", "Dr. A" }, (XLColor?)amarelo),
            (new[] { "", "", "", "", "Dr. B" }, (XLColor?)null),
        });

        PlanilhaLida lida = new LeitorXlsx().Ler(arquivo);

        Assert.NotNull(lida.Linhas[0].ChaveCor);
        Assert.Equal(Situacao.AguardandoAssinaturaProfissional, lida.MapaCores[lida.Linhas[0].ChaveCor!]);
        Assert.Null(lida.Linhas[1].ChaveCor);
    }

    [Fact]
    public void Cor_fora_da_legenda_e_reportada_para_a_usuaria_decidir()
    {
        string arquivo = Caminho("cor-nova.xlsx");
        FabricaDePlanilhas.CriarXlsx(arquivo, new[]
        {
            (new[] { "", "", "", "", "Dr. C" }, (XLColor?)XLColor.FromArgb(255, 1, 2, 3)),
        });

        PlanilhaLida lida = new LeitorXlsx().Ler(arquivo);

        Assert.Single(lida.CoresSemLegenda);
        Assert.Equal(lida.Linhas[0].ChaveCor, lida.CoresSemLegenda[0]);
    }

    [Fact]
    public void Linha_totalmente_vazia_e_ignorada()
    {
        string arquivo = Caminho("vazia-no-meio.xlsx");
        FabricaDePlanilhas.CriarXlsx(arquivo, new[]
        {
            (new[] { "", "", "", "", "Dr. A" }, (XLColor?)null),
            (new[] { "", "", "", "", "" }, (XLColor?)null),
            (new[] { "", "", "", "", "Dr. B" }, (XLColor?)null),
        });

        PlanilhaLida lida = new LeitorXlsx().Ler(arquivo);

        Assert.Equal(2, lida.Linhas.Count);
        Assert.Equal("Dr. B", lida.Linhas[1].Celulas[4]);
        Assert.Equal(4, lida.Linhas[1].Numero); // conserva o número real da linha, para os avisos
    }

    [Fact]
    public void Planilha_sem_aba_de_legenda_le_normalmente_com_mapa_vazio()
    {
        string arquivo = Caminho("sem-legenda.xlsx");
        FabricaDePlanilhas.CriarXlsx(arquivo, new[]
        {
            (new[] { "", "", "", "", "Dr. A" }, (XLColor?)null),
        }, comAbaLegenda: false);

        PlanilhaLida lida = new LeitorXlsx().Ler(arquivo);

        Assert.Empty(lida.MapaCores);
        Assert.Single(lida.Linhas);
    }

    [Fact]
    public void Aceita_xlsx_e_xlsm_e_recusa_csv()
    {
        var leitor = new LeitorXlsx();
        Assert.True(leitor.Aceita("planilha.xlsx"));
        Assert.True(leitor.Aceita("planilha.XLSM"));
        Assert.False(leitor.Aceita("planilha.csv"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }
}
