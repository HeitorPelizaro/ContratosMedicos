namespace ContratosMedicos.Dados.Testes;

public class ServicoBackupTeste : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "cm-backup-" + Guid.NewGuid());
    private readonly CaminhosApp _caminhos;

    public ServicoBackupTeste()
    {
        _caminhos = new CaminhosApp(_pasta);
        _caminhos.GarantirPastas();
        File.WriteAllText(_caminhos.ArquivoBanco, "banco falso");
    }

    [Fact]
    public void Cria_um_backup_por_dia_e_nao_repete()
    {
        var servico = new ServicoBackup(_caminhos);
        var hoje = new DateOnly(2026, 8, 31);

        Assert.NotNull(servico.CriarBackupSeNecessario(hoje));
        Assert.Null(servico.CriarBackupSeNecessario(hoje));
        Assert.Single(Directory.GetFiles(_caminhos.PastaBackups));
    }

    [Fact]
    public void Mantem_apenas_os_sete_backups_mais_recentes()
    {
        var servico = new ServicoBackup(_caminhos);
        for (int i = 0; i < 10; i++)
            servico.CriarBackupSeNecessario(new DateOnly(2026, 8, 1).AddDays(i));

        Assert.Equal(7, Directory.GetFiles(_caminhos.PastaBackups).Length);
        Assert.Contains(Directory.GetFiles(_caminhos.PastaBackups),
            f => Path.GetFileName(f).Contains("2026-08-10"));
        Assert.DoesNotContain(Directory.GetFiles(_caminhos.PastaBackups),
            f => Path.GetFileName(f).Contains("2026-08-01"));
    }

    [Fact]
    public void Backup_avulso_usa_o_sufixo_informado()
    {
        var servico = new ServicoBackup(_caminhos);
        string caminho = servico.CriarBackupAgora("antes-da-importacao");
        Assert.True(File.Exists(caminho));
        Assert.Contains("antes-da-importacao", Path.GetFileName(caminho));
    }

    [Fact]
    public void Importacoes_no_mesmo_dia_nao_comem_o_historico_diario()
    {
        var servico = new ServicoBackup(_caminhos);

        // Sete dias de cópia diária...
        for (int i = 0; i < 7; i++)
            servico.CriarBackupSeNecessario(new DateOnly(2026, 8, 1).AddDays(i));

        // ...e agora dez importações, todas no mesmo dia (nomes forjados para não depender
        // do relógio, que daria o mesmo segundo dez vezes).
        for (int i = 0; i < 10; i++)
            File.Copy(_caminhos.ArquivoBanco, Path.Combine(_caminhos.PastaBackups,
                $"dados-2026-08-07-1200{i:00}-antes-da-importacao.db"));
        servico.CriarBackupAgora("antes-da-importacao");

        string[] nomes = Directory.GetFiles(_caminhos.PastaBackups)
            .Select(Path.GetFileName).ToArray()!;

        // Os 7 dias continuam todos ali.
        for (int i = 0; i < 7; i++)
        {
            string dia = new DateOnly(2026, 8, 1).AddDays(i).ToString("yyyy-MM-dd");
            Assert.Contains($"dados-{dia}.db", nomes);
        }

        // E as importações também rotacionam sozinhas, em 7.
        Assert.Equal(7, nomes.Count(n => n!.Contains("antes-da-importacao")));
    }

    [Fact]
    public void Cada_pote_rotaciona_em_sete_de_forma_independente()
    {
        var servico = new ServicoBackup(_caminhos);

        for (int i = 0; i < 10; i++)
            servico.CriarBackupSeNecessario(new DateOnly(2026, 8, 1).AddDays(i));
        for (int i = 0; i < 10; i++)
            File.Copy(_caminhos.ArquivoBanco, Path.Combine(_caminhos.PastaBackups,
                $"dados-2026-08-1{i}-093000-antes-da-importacao.db"));
        servico.CriarBackupAgora("antes-da-importacao");

        string[] nomes = Directory.GetFiles(_caminhos.PastaBackups)
            .Select(Path.GetFileName).ToArray()!;

        Assert.Equal(7, nomes.Count(n => !n!.Contains("antes-da-importacao")));
        Assert.Equal(7, nomes.Count(n => n!.Contains("antes-da-importacao")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }
}
