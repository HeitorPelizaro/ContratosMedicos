using System.Globalization;
using System.Text;
using ContratosMedicos.Dominio;
using Microsoft.EntityFrameworkCore;

namespace ContratosMedicos.Dados;

/// <summary>
/// Acesso aos contratos para a interface. Os filtros que dependem de regra derivada
/// (vigência efetiva, assinado) são aplicados em memória: são ~670 registros, cabe
/// tudo e evita duplicar a regra em SQL, onde ela sairia de sincronia com o domínio.
///
/// Cada método abre o seu próprio contexto pela fábrica e o descarta no fim. Nada fica
/// rastreado entre operações: o que a tela devolve é sempre um objeto solto, então uma
/// edição abandonada (Cancelar) não tem como ser gravada por um SaveChanges posterior.
/// </summary>
public class RepositorioContratos
{
    private readonly IDbContextFactory<ContratosDbContext> _fabrica;

    public RepositorioContratos(IDbContextFactory<ContratosDbContext> fabrica) => _fabrica = fabrica;

    public List<Contrato> Todos()
    {
        using ContratosDbContext banco = _fabrica.CreateDbContext();
        return banco.Contratos.AsNoTracking()
            .Include(c => c.Aditivos).Include(c => c.Anexos)
            .OrderBy(c => c.Profissional).ToList();
    }

    /// <summary>Cópia solta do contrato: editar o que volta daqui não altera o banco.</summary>
    public Contrato? PorId(int id)
    {
        using ContratosDbContext banco = _fabrica.CreateDbContext();
        return banco.Contratos.AsNoTracking()
            .Include(c => c.Aditivos).Include(c => c.Anexos)
            .FirstOrDefault(c => c.Id == id);
    }

    public ResumoPainel Resumo(DateOnly hoje, int diasAlerta) =>
        CalculadoraResumo.Calcular(Todos(), hoje, diasAlerta);

    public List<Contrato> Listar(FiltroContratos filtro, DateOnly hoje, int diasAlerta)
    {
        IEnumerable<Contrato> consulta = Todos();

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            string busca = Chave(filtro.Busca);
            consulta = consulta.Where(c =>
                Chave(c.Profissional).Contains(busca)
                || Chave(c.Empresa).Contains(busca)
                || Chave(c.Cnpj).Contains(busca)
                || Chave(c.CodigoContratoTasy).Contains(busca)
                || Chave(c.Especialidade).Contains(busca));
        }

        if (filtro.Situacao is not null)
            consulta = consulta.Where(c => c.Situacao == filtro.Situacao);

        if (filtro.Especialidade is not null)
            consulta = consulta.Where(c => c.Especialidade == filtro.Especialidade);

        if (filtro.Empresa is not null)
            consulta = consulta.Where(c => c.Empresa == filtro.Empresa);

        if (filtro.Vigencia != RecorteVigencia.Todos)
            consulta = consulta.Where(c => CasaVigencia(c, filtro.Vigencia, hoje, diasAlerta));

        consulta = filtro.Recorte switch
        {
            RecortePainel.Assinados => consulta.Where(RegrasContrato.EstaAssinado),
            RecortePainel.Pendentes => consulta.Where(RegrasContrato.EstaPendenteDeAssinatura),
            RecortePainel.ComProblema => consulta.Where(RegrasContrato.TemProblema),
            RecortePainel.Inconsistentes => consulta.Where(RegrasContrato.TemInconsistencia),
            RecortePainel.Vencidos => consulta.Where(c =>
                RegrasContrato.ClassificarVigencia(c, hoje, diasAlerta) == ClassificacaoVigencia.Vencida),
            RecortePainel.AVencer => consulta.Where(c =>
                RegrasContrato.ClassificarVigencia(c, hoje, diasAlerta) == ClassificacaoVigencia.AVencer),
            _ => consulta,
        };

        return consulta.ToList();
    }

    public List<string> Especialidades() => ValoresDistintos(c => c.Especialidade);

    public List<string> Empresas() => ValoresDistintos(c => c.Empresa);

    /// <summary>
    /// Grava o contrato que a tela entregou. O objeto que chega é solto (veio de
    /// <see cref="PorId"/> com AsNoTracking), então aqui a gente carrega o que está no
    /// banco e copia campo por campo — inclusive reconciliando a lista de aditivos e de
    /// anexos por Id. Assim gravar é um ato explícito: só o que chega neste método vira dado.
    /// </summary>
    public void Salvar(Contrato contrato, string descricaoHistorico)
    {
        using ContratosDbContext banco = _fabrica.CreateDbContext();

        contrato.AlteradoEm = DateTime.Now;

        if (contrato.Id == 0)
        {
            banco.Contratos.Add(contrato);
            banco.SaveChanges();
        }
        else
        {
            Contrato? destino = banco.Contratos
                .Include(c => c.Aditivos).Include(c => c.Anexos)
                .FirstOrDefault(c => c.Id == contrato.Id);

            if (destino is null)
            {
                // Alguém apagou o contrato enquanto a tela estava aberta: recadastra.
                contrato.Id = 0;
                foreach (Aditivo aditivo in contrato.Aditivos) { aditivo.Id = 0; aditivo.ContratoId = 0; }
                foreach (Anexo anexo in contrato.Anexos) { anexo.Id = 0; anexo.ContratoId = null; }
                banco.Contratos.Add(contrato);
                banco.SaveChanges();
            }
            else
            {
                CopiarEscalares(contrato, destino);
                ReconciliarAditivos(banco, contrato, destino);
                ReconciliarAnexos(banco, contrato, destino);
                banco.SaveChanges();
            }
        }

        if (!string.IsNullOrWhiteSpace(descricaoHistorico))
        {
            banco.Historico.Add(new HistoricoAlteracao
            {
                ContratoId = contrato.Id,
                Descricao = descricaoHistorico,
            });
            banco.SaveChanges();
        }
    }

    public void Excluir(int id)
    {
        using ContratosDbContext banco = _fabrica.CreateDbContext();

        Contrato? contrato = banco.Contratos.Include(c => c.Aditivos).Include(c => c.Anexos)
            .FirstOrDefault(c => c.Id == id);
        if (contrato is null) return;

        banco.Historico.RemoveRange(banco.Historico.Where(h => h.ContratoId == id));
        banco.Contratos.Remove(contrato);
        banco.SaveChanges();
    }

    /// <summary>Anexos do contrato que estão no banco — para apagar os arquivos do disco.</summary>
    public List<Anexo> AnexosDoContrato(int contratoId)
    {
        using ContratosDbContext banco = _fabrica.CreateDbContext();
        return banco.Anexos.AsNoTracking().Where(a => a.ContratoId == contratoId).ToList();
    }

    public List<HistoricoAlteracao> Historico(int contratoId)
    {
        using ContratosDbContext banco = _fabrica.CreateDbContext();
        return banco.Historico.AsNoTracking().Where(h => h.ContratoId == contratoId)
            .OrderByDescending(h => h.QuandoEm).ToList();
    }

    private static void CopiarEscalares(Contrato origem, Contrato destino)
    {
        destino.CadastroRegrasCriterios = origem.CadastroRegrasCriterios;
        destino.Cnpj = origem.Cnpj;
        destino.OptanteSimplesNacional = origem.OptanteSimplesNacional;
        destino.Empresa = origem.Empresa;
        destino.Profissional = origem.Profissional;
        destino.Especialidade = origem.Especialidade;
        destino.AreaAtuacao = origem.AreaAtuacao;
        destino.Edital = origem.Edital;
        destino.CodigoContratoTasy = origem.CodigoContratoTasy;
        destino.ContratoOriginal = origem.ContratoOriginal;
        destino.DataAssinaturaOriginal = origem.DataAssinaturaOriginal;
        destino.VigenciaFim = origem.VigenciaFim;
        destino.Observacao = origem.Observacao;
        destino.Situacao = origem.Situacao;
        destino.AlteradoEm = origem.AlteradoEm;
    }

    private static void ReconciliarAditivos(ContratosDbContext banco, Contrato origem, Contrato destino)
    {
        var idsQueFicam = origem.Aditivos.Where(a => a.Id != 0).Select(a => a.Id).ToHashSet();

        foreach (Aditivo removido in destino.Aditivos.Where(a => !idsQueFicam.Contains(a.Id)).ToList())
        {
            destino.Aditivos.Remove(removido);
            banco.Aditivos.Remove(removido);
        }

        foreach (Aditivo aditivo in origem.Aditivos)
        {
            Aditivo? existente = aditivo.Id == 0
                ? null
                : destino.Aditivos.FirstOrDefault(a => a.Id == aditivo.Id);

            if (existente is null)
            {
                destino.Aditivos.Add(new Aditivo
                {
                    Tipo = aditivo.Tipo,
                    Numero = aditivo.Numero,
                    DataAssinatura = aditivo.DataAssinatura,
                    Situacao = aditivo.Situacao,
                    NovaVigenciaFim = aditivo.NovaVigenciaFim,
                    Valor = aditivo.Valor,
                    Observacao = aditivo.Observacao,
                    CriadoEm = aditivo.CriadoEm,
                });
                continue;
            }

            existente.Tipo = aditivo.Tipo;
            existente.Numero = aditivo.Numero;
            existente.DataAssinatura = aditivo.DataAssinatura;
            existente.Situacao = aditivo.Situacao;
            existente.NovaVigenciaFim = aditivo.NovaVigenciaFim;
            existente.Valor = aditivo.Valor;
            existente.Observacao = aditivo.Observacao;
            // Anexos do aditivo não são editados por esta tela; não mexemos neles de propósito.
        }
    }

    private static void ReconciliarAnexos(ContratosDbContext banco, Contrato origem, Contrato destino)
    {
        var idsQueFicam = origem.Anexos.Where(a => a.Id != 0).Select(a => a.Id).ToHashSet();

        // O FK de Anexo é opcional no modelo: sem o Remove explícito o EF apenas zeraria
        // ContratoId e deixaria uma linha órfã apontando para um PDF sem dono.
        foreach (Anexo removido in destino.Anexos.Where(a => !idsQueFicam.Contains(a.Id)).ToList())
        {
            destino.Anexos.Remove(removido);
            banco.Anexos.Remove(removido);
        }

        foreach (Anexo anexo in origem.Anexos.Where(a => a.Id == 0))
            destino.Anexos.Add(new Anexo
            {
                NomeArquivo = anexo.NomeArquivo,
                CaminhoRelativo = anexo.CaminhoRelativo,
                CriadoEm = anexo.CriadoEm,
            });
    }

    private List<string> ValoresDistintos(Func<Contrato, string?> seletor)
    {
        using ContratosDbContext banco = _fabrica.CreateDbContext();
        return banco.Contratos.AsNoTracking().ToList()
            .Select(seletor)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.CurrentCulture)
            .ToList();
    }

    private static bool CasaVigencia(Contrato contrato, RecorteVigencia recorte, DateOnly hoje, int diasAlerta)
    {
        ClassificacaoVigencia classificacao = RegrasContrato.ClassificarVigencia(contrato, hoje, diasAlerta);
        return recorte switch
        {
            RecorteVigencia.Vencida => classificacao == ClassificacaoVigencia.Vencida,
            RecorteVigencia.AVencer => classificacao == ClassificacaoVigencia.AVencer,
            RecorteVigencia.Vigente => classificacao == ClassificacaoVigencia.Vigente,
            RecorteVigencia.SemData => classificacao == ClassificacaoVigencia.SemData,
            _ => true,
        };
    }

    /// <summary>Busca sem acento e sem caixa — ela vai digitar "angelica" e esperar achar "Angélica".</summary>
    private static string Chave(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return "";
        string decomposto = valor.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);
        foreach (char c in decomposto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                construtor.Append(char.ToUpperInvariant(c));
        return construtor.ToString();
    }
}
