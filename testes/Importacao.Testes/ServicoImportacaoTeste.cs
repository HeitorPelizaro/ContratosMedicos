using ContratosMedicos.Dados;
using ContratosMedicos.Dominio;
using Microsoft.EntityFrameworkCore;

namespace ContratosMedicos.Importacao.Testes;

public class ServicoImportacaoTeste : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "cm-imp-" + Guid.NewGuid());
    private readonly CaminhosApp _caminhos;
    private readonly FabricaContratosDbContext _fabrica;
    private readonly ContratosDbContext _banco;

    public ServicoImportacaoTeste()
    {
        _caminhos = new CaminhosApp(_pasta);
        _caminhos.GarantirPastas();
        _fabrica = new FabricaContratosDbContext(_caminhos);
        _banco = _fabrica.CreateDbContext();
        _banco.Database.EnsureCreated();
    }

    private ServicoImportacao Servico() =>
        new(_fabrica, new ServicoBackup(_caminhos), new ServicoAnexos(_caminhos));

    private static ResultadoValidacao Validacao(params ContratoLido[] contratos) =>
        new(contratos, Array.Empty<AvisoImportacao>(), 0, Array.Empty<string>());

    private static ContratoLido Lido(string tasy, string profissional, string? aditivo = null,
        DateOnly? vigencia = null) =>
        new(new Contrato
        {
            CodigoContratoTasy = tasy,
            Profissional = profissional,
            Situacao = Situacao.Assinado,
            DataAssinaturaOriginal = new DateOnly(2026, 1, 1),
            VigenciaFim = vigencia ?? new DateOnly(2026, 12, 31),
        }, aditivo);

    [Fact]
    public void Insere_contratos_novos()
    {
        ResultadoImportacao resultado = Servico().Gravar(
            Validacao(Lido("T-1", "Dr. A"), Lido("T-2", "Dr. B")),
            ModoImportacao.AdicionarOuAtualizar);

        Assert.Equal(2, resultado.Inseridos);
        Assert.Equal(0, resultado.Atualizados);
        Assert.Equal(2, _banco.Contratos.Count());
    }

    [Fact]
    public void Atualiza_pelo_codigo_tasy_em_vez_de_duplicar()
    {
        Servico().Gravar(Validacao(Lido("T-1", "Dr. A")), ModoImportacao.AdicionarOuAtualizar);

        ResultadoImportacao resultado = Servico().Gravar(
            Validacao(Lido("T-1", "Dr. A Silva", vigencia: new DateOnly(2027, 6, 30))),
            ModoImportacao.AdicionarOuAtualizar);

        Assert.Equal(0, resultado.Inseridos);
        Assert.Equal(1, resultado.Atualizados);
        Contrato contrato = _banco.Contratos.AsNoTracking().Single();
        Assert.Equal("Dr. A Silva", contrato.Profissional);
        Assert.Equal(new DateOnly(2027, 6, 30), contrato.VigenciaFim);
    }

    [Fact]
    public void Substituir_tudo_apaga_o_que_havia_antes()
    {
        Servico().Gravar(Validacao(Lido("T-1", "Dr. A")), ModoImportacao.AdicionarOuAtualizar);

        ResultadoImportacao resultado = Servico().Gravar(
            Validacao(Lido("T-9", "Dr. Z")), ModoImportacao.SubstituirTudo);

        Assert.Equal(1, resultado.Removidos);
        Assert.Equal(1, resultado.Inseridos);
        Assert.Equal("Dr. Z", _banco.Contratos.AsNoTracking().Single().Profissional);
    }

    [Fact]
    public void Texto_da_coluna_aditivo_vira_aditivo_pendente_do_tipo_outro()
    {
        ResultadoImportacao resultado = Servico().Gravar(
            Validacao(Lido("T-1", "Dr. A", aditivo: "T.A. 01/2026 valores")),
            ModoImportacao.AdicionarOuAtualizar);

        Assert.Equal(1, resultado.AditivosCriados);
        Aditivo aditivo = _banco.Aditivos.AsNoTracking().Single();
        Assert.Equal(TipoAditivo.Outro, aditivo.Tipo);
        Assert.Equal(Situacao.NaoClassificado, aditivo.Situacao);
        Assert.Contains("T.A. 01/2026 valores", aditivo.Observacao);
    }

    [Fact]
    public void Coluna_aditivo_vazia_nao_cria_aditivo()
    {
        Servico().Gravar(Validacao(Lido("T-1", "Dr. A", aditivo: "   ")),
            ModoImportacao.AdicionarOuAtualizar);

        Assert.Empty(_banco.Aditivos);
    }

    [Fact]
    public void Faz_backup_antes_de_gravar()
    {
        ResultadoImportacao resultado = Servico().Gravar(
            Validacao(Lido("T-1", "Dr. A")), ModoImportacao.AdicionarOuAtualizar);

        Assert.NotNull(resultado.CaminhoBackup);
        Assert.True(File.Exists(resultado.CaminhoBackup!));
    }

    [Fact]
    public void Sem_codigo_tasy_a_chave_e_cnpj_mais_profissional()
    {
        var primeiro = new ContratoLido(new Contrato
        {
            Cnpj = "12345678000199", Profissional = "Dra. Angélica",
            Situacao = Situacao.Assinado, VigenciaFim = new DateOnly(2026, 12, 31),
        }, null);
        Servico().Gravar(Validacao(primeiro), ModoImportacao.AdicionarOuAtualizar);

        var mesmo = new ContratoLido(new Contrato
        {
            Cnpj = "12345678000199", Profissional = "DRA. ANGELICA",
            Situacao = Situacao.Duvida, VigenciaFim = new DateOnly(2027, 12, 31),
        }, null);
        ResultadoImportacao resultado = Servico().Gravar(Validacao(mesmo), ModoImportacao.AdicionarOuAtualizar);

        Assert.Equal(1, resultado.Atualizados);
        Assert.Equal(1, _banco.Contratos.Count());
    }

    // ---------------------------------------------------------------------------------
    // "Transação única, falha volta ao estado anterior": a garantia que o spec promete.
    // ServicoImportacao expõe AntesDoCommit() só para isto — o teste herda e estoura ali,
    // depois de todos os SaveChanges e antes do Commit.
    // ---------------------------------------------------------------------------------
    private class ImportacaoQueFalhaAntesDoCommit : ServicoImportacao
    {
        public ImportacaoQueFalhaAntesDoCommit(
            FabricaContratosDbContext fabrica, ServicoBackup backup, ServicoAnexos anexos)
            : base(fabrica, backup, anexos) { }

        protected override void AntesDoCommit() =>
            throw new InvalidOperationException("falha simulada no meio da gravação");
    }

    private ServicoImportacao ServicoQueFalha() =>
        new ImportacaoQueFalhaAntesDoCommit(_fabrica, new ServicoBackup(_caminhos), new ServicoAnexos(_caminhos));

    [Fact]
    public void Falha_no_meio_da_gravacao_deixa_o_banco_exatamente_como_estava()
    {
        Servico().Gravar(
            Validacao(Lido("T-1", "Dr. A"), Lido("T-2", "Dr. B", aditivo: "T.A. 01/2026")),
            ModoImportacao.AdicionarOuAtualizar);

        using (ContratosDbContext antes = _fabrica.CreateDbContext())
        {
            Assert.Equal(2, antes.Contratos.Count());
            Assert.Equal(1, antes.Aditivos.Count());
        }

        Assert.Throws<InvalidOperationException>(() => ServicoQueFalha().Gravar(
            Validacao(Lido("T-3", "Dr. C"), Lido("T-1", "Dr. A REESCRITO")),
            ModoImportacao.AdicionarOuAtualizar));

        using ContratosDbContext depois = _fabrica.CreateDbContext();
        Assert.Equal(2, depois.Contratos.Count());
        Assert.Equal(1, depois.Aditivos.Count());
        Assert.Equal("Dr. A", depois.Contratos.AsNoTracking()
            .Single(c => c.CodigoContratoTasy == "T-1").Profissional);
        Assert.Empty(depois.Contratos.Where(c => c.CodigoContratoTasy == "T-3"));
    }

    [Fact]
    public void Falha_no_substituir_tudo_nao_apaga_nada()
    {
        Servico().Gravar(Validacao(Lido("T-1", "Dr. A"), Lido("T-2", "Dr. B")),
            ModoImportacao.AdicionarOuAtualizar);

        using (ContratosDbContext banco = _fabrica.CreateDbContext())
        {
            int id = banco.Contratos.AsNoTracking().First().Id;
            banco.Historico.Add(new HistoricoAlteracao { ContratoId = id, Descricao = "Contrato editado" });
            banco.SaveChanges();
        }

        Assert.Throws<InvalidOperationException>(() => ServicoQueFalha().Gravar(
            Validacao(Lido("T-9", "Dr. Z")), ModoImportacao.SubstituirTudo));

        using ContratosDbContext depois = _fabrica.CreateDbContext();
        Assert.Equal(2, depois.Contratos.Count());
        Assert.Equal(1, depois.Historico.Count());
        Assert.Empty(depois.Contratos.Where(c => c.CodigoContratoTasy == "T-9"));
    }

    [Fact]
    public void Substituir_tudo_leva_o_historico_dos_contratos_apagados()
    {
        Servico().Gravar(Validacao(Lido("T-1", "Dr. A")), ModoImportacao.AdicionarOuAtualizar);

        using (ContratosDbContext banco = _fabrica.CreateDbContext())
        {
            int id = banco.Contratos.AsNoTracking().Single().Id;
            banco.Historico.Add(new HistoricoAlteracao { ContratoId = id, Descricao = "Situação alterada" });
            banco.SaveChanges();
        }

        Servico().Gravar(Validacao(Lido("T-9", "Dr. Z")), ModoImportacao.SubstituirTudo);

        using ContratosDbContext depois = _fabrica.CreateDbContext();
        Assert.Empty(depois.Historico);
    }

    [Fact]
    public void Substituir_tudo_apaga_os_pdfs_dos_contratos_e_aditivos_removidos()
    {
        Servico().Gravar(Validacao(Lido("T-1", "Dr. A", aditivo: "T.A. 01/2026")),
            ModoImportacao.AdicionarOuAtualizar);

        string doContrato = Path.Combine(_caminhos.PastaAnexos, "do-contrato.pdf");
        string doAditivo = Path.Combine(_caminhos.PastaAnexos, "do-aditivo.pdf");
        File.WriteAllText(doContrato, "pdf");
        File.WriteAllText(doAditivo, "pdf");

        using (ContratosDbContext banco = _fabrica.CreateDbContext())
        {
            int idContrato = banco.Contratos.AsNoTracking().Single().Id;
            int idAditivo = banco.Aditivos.AsNoTracking().Single().Id;
            banco.Anexos.AddRange(
                new Anexo { ContratoId = idContrato, NomeArquivo = "c.pdf", CaminhoRelativo = "do-contrato.pdf" },
                new Anexo { AditivoId = idAditivo, NomeArquivo = "a.pdf", CaminhoRelativo = "do-aditivo.pdf" });
            banco.SaveChanges();
        }

        Servico().Gravar(Validacao(Lido("T-9", "Dr. Z")), ModoImportacao.SubstituirTudo);

        using ContratosDbContext depois = _fabrica.CreateDbContext();
        Assert.Empty(depois.Anexos);
        Assert.False(File.Exists(doContrato));
        Assert.False(File.Exists(doAditivo));
    }

    [Fact]
    public void Edicao_de_contrato_abandonada_na_tela_nao_entra_na_importacao()
    {
        Servico().Gravar(Validacao(Lido("T-1", "Dr. A")), ModoImportacao.AdicionarOuAtualizar);

        var repositorio = new RepositorioContratos(_fabrica);
        int id = repositorio.Todos().Single().Id;

        // Tela aberta, campos mexidos, Cancelar: nada de Salvar.
        Contrato abandonado = repositorio.PorId(id)!;
        abandonado.Profissional = "NOME QUE NUNCA FOI SALVO";

        Servico().Gravar(Validacao(Lido("T-2", "Dr. B")), ModoImportacao.AdicionarOuAtualizar);

        Assert.Equal("Dr. A", repositorio.PorId(id)!.Profissional);
    }

    public void Dispose()
    {
        _banco.Dispose();
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }
}
