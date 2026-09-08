using Microsoft.EntityFrameworkCore;

namespace ContratosMedicos.Dados;

/// <summary>
/// Fábrica de contexto curta: cada operação abre o seu próprio <see cref="ContratosDbContext"/>,
/// usa e descarta. É o padrão recomendado no Blazor Server — o escopo de injeção lá é o circuito
/// (a sessão inteira), então um contexto injetado viveria horas acumulando alterações
/// não salvas. Com a fábrica, edição abandonada morre junto com o contexto.
/// Em produção o registro vem de AddDbContextFactory; esta classe existe para os testes
/// e para cenários fora do host web.
/// </summary>
public class FabricaContratosDbContext : IDbContextFactory<ContratosDbContext>
{
    private readonly DbContextOptions<ContratosDbContext> _opcoes;

    public FabricaContratosDbContext(DbContextOptions<ContratosDbContext> opcoes) => _opcoes = opcoes;

    public FabricaContratosDbContext(CaminhosApp caminhos)
        : this(new DbContextOptionsBuilder<ContratosDbContext>()
            .UseSqlite(caminhos.StringDeConexao).Options)
    {
    }

    public ContratosDbContext CreateDbContext() => new(_opcoes);
}
