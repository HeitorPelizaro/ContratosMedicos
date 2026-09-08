using ContratosMedicos.Dominio;
using Microsoft.EntityFrameworkCore;

namespace ContratosMedicos.Dados;

public class ContratosDbContext : DbContext
{
    public ContratosDbContext(DbContextOptions<ContratosDbContext> opcoes) : base(opcoes) { }

    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Aditivo> Aditivos => Set<Aditivo>();
    public DbSet<Anexo> Anexos => Set<Anexo>();
    public DbSet<HistoricoAlteracao> Historico => Set<HistoricoAlteracao>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<FotoMensal> FotosMensais => Set<FotoMensal>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Contrato>(e =>
        {
            e.HasIndex(c => c.CodigoContratoTasy);
            e.HasIndex(c => c.Profissional);
            e.HasIndex(c => c.VigenciaFim);
            e.HasMany(c => c.Aditivos).WithOne().HasForeignKey(a => a.ContratoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Anexos).WithOne().HasForeignKey(a => a.ContratoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(c => c.Situacao).HasConversion<int>();
        });

        modelo.Entity<Aditivo>(e =>
        {
            e.Property(a => a.Tipo).HasConversion<int>();
            e.Property(a => a.Situacao).HasConversion<int>();
            e.Property(a => a.Valor).HasColumnType("decimal(18,2)");
            e.HasMany(a => a.Anexos).WithOne().HasForeignKey(a => a.AditivoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<HistoricoAlteracao>().HasIndex(h => h.ContratoId);
        modelo.Entity<Configuracao>().HasKey(c => c.Chave);
        modelo.Entity<FotoMensal>().HasKey(f => new { f.Ano, f.Mes });
    }
}
