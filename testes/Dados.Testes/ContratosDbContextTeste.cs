using ContratosMedicos.Dominio;
using Microsoft.EntityFrameworkCore;

namespace ContratosMedicos.Dados.Testes;

public class ContratosDbContextTeste : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "cm-testes-" + Guid.NewGuid());

    private ContratosDbContext AbrirBanco()
    {
        var caminhos = new CaminhosApp(_pasta);
        caminhos.GarantirPastas();
        var opcoes = new DbContextOptionsBuilder<ContratosDbContext>()
            .UseSqlite(caminhos.StringDeConexao).Options;
        var banco = new ContratosDbContext(opcoes);
        banco.Database.EnsureCreated();
        return banco;
    }

    [Fact]
    public void Grava_e_le_contrato_com_aditivos_datas_e_valores()
    {
        using (ContratosDbContext banco = AbrirBanco())
        {
            banco.Contratos.Add(new Contrato
            {
                Profissional = "Angélica Formiga",
                Cnpj = "12345678000199",
                OptanteSimplesNacional = true,
                Situacao = Situacao.Assinado,
                DataAssinaturaOriginal = new DateOnly(2026, 1, 10),
                VigenciaFim = new DateOnly(2026, 12, 31),
                Aditivos =
                {
                    new Aditivo
                    {
                        Tipo = TipoAditivo.Valor,
                        DataAssinatura = new DateOnly(2026, 7, 15),
                        Situacao = Situacao.Assinado,
                        Valor = 12500.50m,
                    },
                },
            });
            banco.SaveChanges();
        }

        using (ContratosDbContext banco = AbrirBanco())
        {
            Contrato contrato = banco.Contratos.Include(c => c.Aditivos).Single();
            Assert.Equal("Angélica Formiga", contrato.Profissional);
            Assert.True(contrato.OptanteSimplesNacional);
            Assert.Equal(new DateOnly(2026, 12, 31), contrato.VigenciaFim);
            Assert.Equal(12500.50m, contrato.Aditivos.Single().Valor);
            Assert.Equal(new DateOnly(2026, 7, 15), contrato.Aditivos.Single().DataAssinatura);
        }
    }

    [Fact]
    public void Excluir_contrato_leva_os_aditivos_e_anexos()
    {
        using ContratosDbContext banco = AbrirBanco();
        var contrato = new Contrato
        {
            Profissional = "Teste",
            Aditivos = { new Aditivo { Tipo = TipoAditivo.Outro } },
            Anexos = { new Anexo { NomeArquivo = "a.pdf", CaminhoRelativo = "a.pdf" } },
        };
        banco.Contratos.Add(contrato);
        banco.SaveChanges();

        banco.Contratos.Remove(contrato);
        banco.SaveChanges();

        Assert.Empty(banco.Aditivos);
        Assert.Empty(banco.Anexos);
    }

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }
}
