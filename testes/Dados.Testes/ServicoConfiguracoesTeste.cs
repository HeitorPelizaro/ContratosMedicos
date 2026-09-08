using Microsoft.EntityFrameworkCore;

namespace ContratosMedicos.Dados.Testes;

public class ServicoConfiguracoesTeste : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "cm-cfg-" + Guid.NewGuid());
    private readonly FabricaContratosDbContext _fabrica;
    private readonly ContratosDbContext _banco;

    public ServicoConfiguracoesTeste()
    {
        var caminhos = new CaminhosApp(_pasta);
        caminhos.GarantirPastas();
        _fabrica = new FabricaContratosDbContext(caminhos);
        _banco = _fabrica.CreateDbContext();
        _banco.Database.EnsureCreated();
    }

    [Fact]
    public void Dias_de_alerta_comeca_em_trinta()
    {
        Assert.Equal(30, new ServicoConfiguracoes(_fabrica).DiasAlerta);
    }

    [Fact]
    public void Dias_de_alerta_persiste_o_valor_escolhido()
    {
        var servico = new ServicoConfiguracoes(_fabrica);
        servico.DefinirDiasAlerta(90);
        Assert.Equal(90, new ServicoConfiguracoes(_fabrica).DiasAlerta);
    }

    [Fact]
    public void Dias_de_alerta_rejeita_valor_invalido()
    {
        var servico = new ServicoConfiguracoes(_fabrica);
        Assert.Throws<ArgumentOutOfRangeException>(() => servico.DefinirDiasAlerta(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => servico.DefinirDiasAlerta(400));
    }

    public void Dispose()
    {
        _banco.Dispose();
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }
}
