using ContratosMedicos.Dominio;
using Microsoft.EntityFrameworkCore;

namespace ContratosMedicos.Dados.Testes;

public class RepositorioContratosTeste : IDisposable
{
    private static readonly DateOnly Hoje = new(2026, 8, 31);
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "cm-repo-" + Guid.NewGuid());
    private readonly FabricaContratosDbContext _fabrica;
    private readonly ContratosDbContext _banco;
    private readonly RepositorioContratos _repositorio;

    public RepositorioContratosTeste()
    {
        var caminhos = new CaminhosApp(_pasta);
        caminhos.GarantirPastas();
        _fabrica = new FabricaContratosDbContext(caminhos);
        _banco = _fabrica.CreateDbContext();
        _banco.Database.EnsureCreated();
        _repositorio = new RepositorioContratos(_fabrica);

        _banco.Contratos.AddRange(
            new Contrato
            {
                Profissional = "Dra. Angélica Formiga", Empresa = "Clínica Alfa",
                Especialidade = "Cardiologia", Cnpj = "12345678000199", CodigoContratoTasy = "TASY-1",
                Situacao = Situacao.Assinado, DataAssinaturaOriginal = new DateOnly(2026, 1, 1),
                VigenciaFim = new DateOnly(2026, 9, 10),
            },
            new Contrato
            {
                Profissional = "Dr. Bruno", Empresa = "Clínica Beta", Especialidade = "Ortopedia",
                Situacao = Situacao.AguardandoAssinaturaProfissional, VigenciaFim = new DateOnly(2026, 7, 1),
            },
            new Contrato
            {
                Profissional = "Dra. Carla", Empresa = "Clínica Alfa", Especialidade = "Cardiologia",
                Situacao = Situacao.Duvida, VigenciaFim = null,
            });
        _banco.SaveChanges();
    }

    [Fact]
    public void Busca_encontra_por_pedaco_do_nome_sem_acento_e_sem_caixa()
    {
        List<Contrato> achados = _repositorio.Listar(
            new FiltroContratos { Busca = "angelica" }, Hoje, 30);
        Assert.Equal("Dra. Angélica Formiga", Assert.Single(achados).Profissional);
    }

    [Fact]
    public void Busca_tambem_olha_empresa_cnpj_e_codigo_tasy()
    {
        Assert.Single(_repositorio.Listar(new FiltroContratos { Busca = "Beta" }, Hoje, 30));
        Assert.Single(_repositorio.Listar(new FiltroContratos { Busca = "12345678" }, Hoje, 30));
        Assert.Single(_repositorio.Listar(new FiltroContratos { Busca = "tasy-1" }, Hoje, 30));
    }

    [Fact]
    public void Filtra_por_situacao_especialidade_e_empresa()
    {
        Assert.Single(_repositorio.Listar(new FiltroContratos { Situacao = Situacao.Duvida }, Hoje, 30));
        Assert.Equal(2, _repositorio.Listar(new FiltroContratos { Especialidade = "Cardiologia" }, Hoje, 30).Count);
        Assert.Equal(2, _repositorio.Listar(new FiltroContratos { Empresa = "Clínica Alfa" }, Hoje, 30).Count);
    }

    [Fact]
    public void Filtra_por_recorte_de_vigencia()
    {
        Assert.Single(_repositorio.Listar(new FiltroContratos { Vigencia = RecorteVigencia.Vencida }, Hoje, 30));
        Assert.Single(_repositorio.Listar(new FiltroContratos { Vigencia = RecorteVigencia.AVencer }, Hoje, 30));
        Assert.Single(_repositorio.Listar(new FiltroContratos { Vigencia = RecorteVigencia.SemData }, Hoje, 30));
    }

    [Fact]
    public void Recortes_do_painel_batem_com_os_cartoes()
    {
        Assert.Single(_repositorio.Listar(new FiltroContratos { Recorte = RecortePainel.Assinados }, Hoje, 30));
        Assert.Single(_repositorio.Listar(new FiltroContratos { Recorte = RecortePainel.Pendentes }, Hoje, 30));
        Assert.Single(_repositorio.Listar(new FiltroContratos { Recorte = RecortePainel.ComProblema }, Hoje, 30));
        Assert.Single(_repositorio.Listar(new FiltroContratos { Recorte = RecortePainel.Vencidos }, Hoje, 30));
    }

    [Fact]
    public void Filtros_se_combinam()
    {
        List<Contrato> achados = _repositorio.Listar(new FiltroContratos
        {
            Especialidade = "Cardiologia",
            Recorte = RecortePainel.ComProblema,
        }, Hoje, 30);

        Assert.Equal("Dra. Carla", Assert.Single(achados).Profissional);
    }

    [Fact]
    public void Resumo_usa_os_contratos_do_banco()
    {
        ResumoPainel resumo = _repositorio.Resumo(Hoje, 30);
        Assert.Equal(3, resumo.Total);
        Assert.Equal(1, resumo.Assinados);
        Assert.Equal(1, resumo.Vencidos);
    }

    [Fact]
    public void Listas_de_especialidades_e_empresas_vem_ordenadas_e_sem_repeticao()
    {
        Assert.Equal(new[] { "Cardiologia", "Ortopedia" }, _repositorio.Especialidades());
        Assert.Equal(new[] { "Clínica Alfa", "Clínica Beta" }, _repositorio.Empresas());
    }

    [Fact]
    public void Salvar_registra_o_historico_em_portugues()
    {
        Contrato contrato = _repositorio.Listar(new FiltroContratos { Busca = "Bruno" }, Hoje, 30)[0];
        contrato.Situacao = Situacao.Assinado;
        contrato.DataAssinaturaOriginal = new DateOnly(2026, 8, 30);

        _repositorio.Salvar(contrato, "Situação alterada para Assinado");

        HistoricoAlteracao registro = Assert.Single(_repositorio.Historico(contrato.Id));
        Assert.Equal("Situação alterada para Assinado", registro.Descricao);
    }

    [Fact]
    public void Excluir_remove_o_contrato()
    {
        int id = _repositorio.Todos()[0].Id;
        _repositorio.Excluir(id);
        Assert.Null(_repositorio.PorId(id));
        Assert.Equal(2, _repositorio.Todos().Count);
    }

    // ---------------------------------------------------------------------------------
    // Regressão do achado crítico: o DbContext era Scoped e no Blazor interativo por
    // servidor o escopo é o circuito inteiro. PorId devolvia a entidade RASTREADA, a tela
    // editava direto nela e "Cancelar" só escondia o painel — a edição abandonada ficava
    // suja no change tracker e era gravada pelo SaveChanges seguinte de qualquer outra tela.
    // ---------------------------------------------------------------------------------

    [Fact]
    public void PorId_devolve_copia_solta_editar_o_retorno_nao_encosta_no_banco()
    {
        int id = _repositorio.Listar(new FiltroContratos { Busca = "Bruno" }, Hoje, 30)[0].Id;

        Contrato naTela = _repositorio.PorId(id)!;
        naTela.Profissional = "NOME QUE NUNCA FOI SALVO";

        Assert.Equal("Dr. Bruno", _repositorio.PorId(id)!.Profissional);
    }

    [Fact]
    public void Edicao_cancelada_nao_e_gravada_por_um_salvamento_posterior_sem_relacao()
    {
        int idAbandonado = _repositorio.Listar(new FiltroContratos { Busca = "Bruno" }, Hoje, 30)[0].Id;
        int idOutro = _repositorio.Listar(new FiltroContratos { Busca = "Carla" }, Hoje, 30)[0].Id;

        // A usuária abre o contrato do Dr. Bruno, mexe em tudo e clica em Cancelar:
        // ou seja, Salvar NUNCA é chamado para este contrato.
        Contrato abandonado = _repositorio.PorId(idAbandonado)!;
        abandonado.Profissional = "NOME QUE NUNCA FOI SALVO";
        abandonado.Situacao = Situacao.Assinado;
        abandonado.VigenciaFim = new DateOnly(2099, 1, 1);
        abandonado.Aditivos.Add(new Aditivo { Tipo = TipoAditivo.Vigencia });
        abandonado.Anexos.Add(new Anexo { NomeArquivo = "fantasma.pdf", CaminhoRelativo = "fantasma.pdf" });

        // Agora vários SaveChanges de outras telas, um de cada origem que existe no app.
        Contrato outro = _repositorio.PorId(idOutro)!;
        outro.Observacao = "editado de propósito";
        _repositorio.Salvar(outro, "Contrato editado");                          // outra tela de contrato
        new ServicoConfiguracoes(_fabrica).DefinirDiasAlerta(45);                 // Configurações / importação
        new RepositorioIndicadores(_fabrica, _repositorio)
            .Obter(2026, 7, Hoje);                                                // tela de Indicadores

        // O contrato abandonado continua exatamente como estava no banco.
        Contrato doBanco = _repositorio.PorId(idAbandonado)!;
        Assert.Equal("Dr. Bruno", doBanco.Profissional);
        Assert.Equal(Situacao.AguardandoAssinaturaProfissional, doBanco.Situacao);
        Assert.Equal(new DateOnly(2026, 7, 1), doBanco.VigenciaFim);
        Assert.Empty(doBanco.Aditivos);
        Assert.Empty(doBanco.Anexos);

        // E a lista da tela de Contratos também não mostra a edição abandonada.
        Assert.DoesNotContain(_repositorio.Todos(), c => c.Profissional == "NOME QUE NUNCA FOI SALVO");

        // O salvamento legítimo, esse sim, foi gravado.
        Assert.Equal("editado de propósito", _repositorio.PorId(idOutro)!.Observacao);
    }

    [Fact]
    public void Salvar_grava_a_copia_recebida_com_aditivo_e_anexo_novos()
    {
        int id = _repositorio.Listar(new FiltroContratos { Busca = "Bruno" }, Hoje, 30)[0].Id;

        Contrato naTela = _repositorio.PorId(id)!;
        naTela.Profissional = "Dr. Bruno Souza";
        naTela.Aditivos.Add(new Aditivo
        {
            Tipo = TipoAditivo.Vigencia,
            Situacao = Situacao.Assinado,
            DataAssinatura = new DateOnly(2026, 8, 1),
            NovaVigenciaFim = new DateOnly(2027, 7, 1),
        });
        naTela.Anexos.Add(new Anexo { NomeArquivo = "contrato.pdf", CaminhoRelativo = "abc.pdf" });

        _repositorio.Salvar(naTela, "Contrato editado");

        Contrato doBanco = _repositorio.PorId(id)!;
        Assert.Equal("Dr. Bruno Souza", doBanco.Profissional);
        Assert.Equal(new DateOnly(2027, 7, 1), Assert.Single(doBanco.Aditivos).NovaVigenciaFim);
        Assert.Equal("contrato.pdf", Assert.Single(doBanco.Anexos).NomeArquivo);
    }

    [Fact]
    public void Salvar_remove_do_banco_o_aditivo_e_o_anexo_tirados_na_tela()
    {
        int id = _repositorio.Listar(new FiltroContratos { Busca = "Bruno" }, Hoje, 30)[0].Id;

        Contrato naTela = _repositorio.PorId(id)!;
        naTela.Aditivos.Add(new Aditivo { Tipo = TipoAditivo.Valor, Valor = 100m });
        naTela.Anexos.Add(new Anexo { NomeArquivo = "x.pdf", CaminhoRelativo = "x.pdf" });
        _repositorio.Salvar(naTela, "");

        Contrato paraLimpar = _repositorio.PorId(id)!;
        paraLimpar.Aditivos.Clear();
        paraLimpar.Anexos.Clear();
        _repositorio.Salvar(paraLimpar, "");

        Contrato doBanco = _repositorio.PorId(id)!;
        Assert.Empty(doBanco.Aditivos);
        Assert.Empty(doBanco.Anexos);
        Assert.Empty(_fabrica.CreateDbContext().Aditivos);
        Assert.Empty(_fabrica.CreateDbContext().Anexos);
    }

    [Fact]
    public void Salvar_novo_contrato_continua_devolvendo_o_id_gerado()
    {
        var novo = new Contrato { Profissional = "Dr. Novo", Especialidade = "Neurologia" };
        _repositorio.Salvar(novo, "Contrato cadastrado");

        Assert.NotEqual(0, novo.Id);
        Assert.Equal("Dr. Novo", _repositorio.PorId(novo.Id)!.Profissional);
        Assert.Single(_repositorio.Historico(novo.Id));
    }

    public void Dispose()
    {
        _banco.Dispose();
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }
}
