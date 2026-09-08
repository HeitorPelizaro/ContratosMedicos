using ContratosMedicos.Dominio;
using Microsoft.EntityFrameworkCore;

namespace ContratosMedicos.Dados.Testes;

public class RepositorioIndicadoresTeste : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "cm-ind-" + Guid.NewGuid());
    private readonly FabricaContratosDbContext _fabrica;
    private readonly ContratosDbContext _banco;
    private readonly RepositorioIndicadores _indicadores;

    public RepositorioIndicadoresTeste()
    {
        var caminhos = new CaminhosApp(_pasta);
        caminhos.GarantirPastas();
        _fabrica = new FabricaContratosDbContext(caminhos);
        _banco = _fabrica.CreateDbContext();
        _banco.Database.EnsureCreated();
        _indicadores = new RepositorioIndicadores(_fabrica, new RepositorioContratos(_fabrica));

        _banco.Contratos.AddRange(
            new Contrato { Profissional = "Dra. Angélica", Especialidade = "Cardiologia",
                DataAssinaturaOriginal = new DateOnly(2026, 7, 10), Situacao = Situacao.Assinado },
            new Contrato { Profissional = "Dr. Bruno", Especialidade = "Ortopedia",
                Situacao = Situacao.AguardandoAssinaturaProfissional });
        _banco.SaveChanges();
    }

    [Fact]
    public void Mes_fechado_sem_foto_e_calculado_e_fotografado_na_hora()
    {
        IndicadorApresentado apresentado = _indicadores.Obter(2026, 7, new DateOnly(2026, 8, 31));

        Assert.Equal(2, apresentado.Indicadores.MedicosDistintos);
        Assert.Equal(1, apresentado.Indicadores.ContratosAssinadosAte);
        Assert.True(apresentado.VeioDeFoto);
        Assert.Single(_banco.Set<FotoMensal>());
    }

    [Fact]
    public void Foto_gravada_prevalece_mesmo_depois_de_o_cadastro_mudar()
    {
        _indicadores.Obter(2026, 7, new DateOnly(2026, 8, 31));

        _banco.Contratos.Add(new Contrato { Profissional = "Dr. Novo",
            DataAssinaturaOriginal = new DateOnly(2026, 7, 5), Situacao = Situacao.Assinado });
        _banco.SaveChanges();

        IndicadorApresentado apresentado = _indicadores.Obter(2026, 7, new DateOnly(2026, 8, 31));

        Assert.Equal(2, apresentado.Indicadores.MedicosDistintos);   // continua a foto
        Assert.Equal(1, apresentado.Indicadores.ContratosAssinadosAte);
        Assert.NotNull(apresentado.FotografadoEm);
    }

    [Fact]
    public void Mes_corrente_e_sempre_calculado_e_nunca_fotografado()
    {
        IndicadorApresentado apresentado = _indicadores.Obter(2026, 8, new DateOnly(2026, 8, 31));

        Assert.False(apresentado.VeioDeFoto);
        Assert.Null(apresentado.FotografadoEm);
        Assert.Empty(_banco.Set<FotoMensal>());
    }

    [Fact]
    public void Mes_futuro_e_calculado_e_nao_fotografado()
    {
        IndicadorApresentado apresentado = _indicadores.Obter(2026, 12, new DateOnly(2026, 8, 31));

        Assert.False(apresentado.VeioDeFoto);
        Assert.Empty(_banco.Set<FotoMensal>());
    }

    [Fact]
    public void Fotografar_duas_vezes_o_mesmo_mes_atualiza_em_vez_de_duplicar()
    {
        IndicadoresMes calculado = CalculadoraIndicadores.Calcular(
            new RepositorioContratos(_fabrica).Todos(), 2026, 6);

        _indicadores.Fotografar(calculado);
        _indicadores.Fotografar(calculado);

        Assert.Single(_banco.Set<FotoMensal>());
    }

    [Fact]
    public void A_quebra_por_especialidade_da_foto_e_recalculada_com_aviso()
    {
        // A foto guarda só os totais; a quebra por especialidade é sempre a atual.
        IndicadorApresentado apresentado = _indicadores.Obter(2026, 7, new DateOnly(2026, 8, 31));

        Assert.Equal(2, apresentado.Indicadores.PorEspecialidade.Count);
    }

    [Fact]
    public void Mes_fechado_sem_contrato_nenhum_nao_e_fotografado()
    {
        // Abrir a tela de Indicadores antes da primeira importação não pode congelar
        // o mês passado em zero para sempre — não há como desfazer isso pela interface.
        using (ContratosDbContext limpar = _fabrica.CreateDbContext())
        {
            limpar.Contratos.RemoveRange(limpar.Contratos);
            limpar.SaveChanges();
        }

        IndicadorApresentado apresentado = _indicadores.Obter(2026, 7, new DateOnly(2026, 8, 31));

        Assert.False(apresentado.VeioDeFoto);
        Assert.Null(apresentado.FotografadoEm);
        Assert.Empty(_fabrica.CreateDbContext().Set<FotoMensal>());
    }

    [Fact]
    public void Depois_que_os_dados_chegam_o_mes_fechado_volta_a_ser_fotografado()
    {
        using (ContratosDbContext limpar = _fabrica.CreateDbContext())
        {
            limpar.Contratos.RemoveRange(limpar.Contratos);
            limpar.SaveChanges();
        }
        _indicadores.Obter(2026, 7, new DateOnly(2026, 8, 31));
        Assert.Empty(_fabrica.CreateDbContext().Set<FotoMensal>());

        using (ContratosDbContext banco = _fabrica.CreateDbContext())
        {
            banco.Contratos.Add(new Contrato { Profissional = "Dra. Angélica",
                DataAssinaturaOriginal = new DateOnly(2026, 7, 10), Situacao = Situacao.Assinado });
            banco.SaveChanges();
        }

        IndicadorApresentado apresentado = _indicadores.Obter(2026, 7, new DateOnly(2026, 8, 31));

        Assert.True(apresentado.VeioDeFoto);
        Assert.Equal(1, apresentado.Indicadores.ContratosTotal);
    }

    [Fact]
    public void Refazer_a_foto_troca_os_numeros_congelados_pelos_de_agora()
    {
        _indicadores.Obter(2026, 7, new DateOnly(2026, 8, 31));

        using (ContratosDbContext banco = _fabrica.CreateDbContext())
        {
            banco.Contratos.Add(new Contrato { Profissional = "Dr. Novo", Especialidade = "Pediatria",
                DataAssinaturaOriginal = new DateOnly(2026, 7, 5), Situacao = Situacao.Assinado });
            banco.SaveChanges();
        }

        // Sem refazer, a foto antiga continua mandando.
        Assert.Equal(2, _indicadores.Obter(2026, 7, new DateOnly(2026, 8, 31)).Indicadores.MedicosDistintos);

        _indicadores.RefazerFoto(2026, 7);

        IndicadorApresentado depois = _indicadores.Obter(2026, 7, new DateOnly(2026, 8, 31));
        Assert.True(depois.VeioDeFoto);
        Assert.Equal(3, depois.Indicadores.MedicosDistintos);
        Assert.Equal(2, depois.Indicadores.ContratosAssinadosAte);
        Assert.Single(_fabrica.CreateDbContext().Set<FotoMensal>());
    }

    public void Dispose()
    {
        _banco.Dispose();
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }
}
