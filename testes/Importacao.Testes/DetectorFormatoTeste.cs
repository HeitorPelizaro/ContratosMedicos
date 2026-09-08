using ClosedXML.Excel;

namespace ContratosMedicos.Importacao.Testes;

public class DetectorFormatoTeste : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "cm-det-" + Guid.NewGuid());

    public DetectorFormatoTeste() => Directory.CreateDirectory(_pasta);

    [Fact]
    public void Xlsx_com_extensao_errada_ainda_e_lido_como_xlsx()
    {
        string arquivo = Path.Combine(_pasta, "planilha.csv"); // extensão mentindo
        FabricaDePlanilhas.CriarXlsx(arquivo, new[]
        {
            (new[] { "", "", "", "", "Dr. A" }, (XLColor?)null),
        });

        Assert.IsType<LeitorXlsx>(DetectorFormato.Escolher(arquivo));
    }

    [Fact]
    public void Csv_de_verdade_usa_o_leitor_de_csv()
    {
        string arquivo = Path.Combine(_pasta, "dados.csv");
        File.WriteAllText(arquivo, "PROFISSIONAL;VIGÊNCIA\nDr. A;31/12/2026\n");

        Assert.IsType<LeitorCsv>(DetectorFormato.Escolher(arquivo));
    }

    [Fact]
    public void Arquivo_nao_suportado_avisa_em_portugues()
    {
        string arquivo = Path.Combine(_pasta, "foto.png");
        File.WriteAllBytes(arquivo, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0, 0, 0, 0 });

        NotSupportedException erro = Assert.Throws<NotSupportedException>(
            () => DetectorFormato.Escolher(arquivo));
        Assert.Contains("planilha", erro.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }
}
