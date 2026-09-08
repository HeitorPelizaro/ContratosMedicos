using ContratosMedicos.Dados;
using ContratosMedicos.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ContratosMedicos.Importacao;

/// <summary>
/// Grava a importação numa transação, depois de tirar backup do banco.
/// Se algo estourar no meio, nada é gravado e o backup continua ali.
///
/// O contexto é criado aqui, pela fábrica, e vive só o tempo da gravação. Isso é o que
/// garante de verdade o "falha volta ao estado anterior": a transação enxerga apenas o
/// que este método escreveu, nunca alterações pendentes de outra tela.
/// </summary>
public class ServicoImportacao
{
    private readonly IDbContextFactory<ContratosDbContext> _fabrica;
    private readonly ServicoBackup _backup;
    private readonly ServicoAnexos _anexos;

    public ServicoImportacao(
        IDbContextFactory<ContratosDbContext> fabrica, ServicoBackup backup, ServicoAnexos anexos)
    {
        _fabrica = fabrica;
        _backup = backup;
        _anexos = anexos;
    }

    public ResultadoImportacao Gravar(ResultadoValidacao validacao, ModoImportacao modo)
    {
        string? caminhoBackup = _backup.CriarBackupAgora("antes-da-importacao");

        using ContratosDbContext banco = _fabrica.CreateDbContext();
        using IDbContextTransaction transacao = banco.Database.BeginTransaction();

        int removidos = 0;
        List<Anexo> anexosParaApagarDoDisco = new();

        if (modo == ModoImportacao.SubstituirTudo)
        {
            List<Contrato> existentes = banco.Contratos
                .Include(c => c.Aditivos).Include(c => c.Anexos).ToList();
            removidos = existentes.Count;

            // Os PDFs dos contratos e dos aditivos que vão embora. Guardamos a lista e só
            // apagamos os arquivos depois do commit: se a transação voltar atrás, os arquivos
            // ainda precisam existir para as linhas que continuam no banco.
            List<int> idsAditivos = existentes.SelectMany(c => c.Aditivos).Select(a => a.Id).ToList();
            anexosParaApagarDoDisco.AddRange(existentes.SelectMany(c => c.Anexos));
            anexosParaApagarDoDisco.AddRange(banco.Anexos.AsNoTracking()
                .Where(a => a.AditivoId != null && idsAditivos.Contains(a.AditivoId.Value))
                .ToList());

            // O cascade leva aditivos e anexos, mas NÃO o histórico (ele não tem relação
            // mapeada com Contrato). Sem isto o histórico ficaria órfão e invisível no banco.
            var idsContratos = existentes.Select(c => c.Id).ToList();
            banco.Historico.RemoveRange(banco.Historico.Where(h => idsContratos.Contains(h.ContratoId)));

            banco.Contratos.RemoveRange(existentes);
            banco.SaveChanges();
        }

        List<Contrato> naBase = banco.Contratos.Include(c => c.Aditivos).ToList();
        Dictionary<string, Contrato> porChave = new();
        foreach (Contrato contrato in naBase)
        {
            string? chave = Chave(contrato);
            if (chave is not null) porChave[chave] = contrato;
        }

        int inseridos = 0, atualizados = 0, aditivosCriados = 0;

        foreach (ContratoLido lido in validacao.Contratos)
        {
            string? chave = Chave(lido.Contrato);
            Contrato destino;

            if (chave is not null && porChave.TryGetValue(chave, out Contrato? existente))
            {
                CopiarCampos(lido.Contrato, existente);
                existente.AlteradoEm = DateTime.Now;
                destino = existente;
                atualizados++;
            }
            else
            {
                destino = lido.Contrato;
                banco.Contratos.Add(destino);
                if (chave is not null) porChave[chave] = destino;
                inseridos++;
            }

            string? textoAditivo = NormalizadorValores.LerTexto(lido.AditivoTexto);
            if (textoAditivo is not null && !destino.Aditivos.Any(a => a.Observacao == textoAditivo))
            {
                destino.Aditivos.Add(new Aditivo
                {
                    Tipo = TipoAditivo.Outro,
                    Situacao = Situacao.NaoClassificado,
                    Observacao = textoAditivo,
                });
                aditivosCriados++;
            }
        }

        banco.SaveChanges();
        AntesDoCommit();
        transacao.Commit();

        foreach (Anexo anexo in anexosParaApagarDoDisco) _anexos.Excluir(anexo);

        return new ResultadoImportacao(inseridos, atualizados, removidos, aditivosCriados, caminhoBackup);
    }

    /// <summary>
    /// Ponto de extensão usado pelos testes para simular uma falha no meio da gravação,
    /// depois dos SaveChanges e antes do commit. Em produção não faz nada.
    /// </summary>
    protected virtual void AntesDoCommit()
    {
    }

    /// <summary>Código Tasy quando existe; senão CNPJ + profissional normalizado.</summary>
    private static string? Chave(Contrato contrato)
    {
        string? tasy = NormalizadorValores.LerTexto(contrato.CodigoContratoTasy);
        if (tasy is not null) return "tasy:" + Texto.Normalizar(tasy);

        string? cnpj = NormalizadorValores.LerCnpj(contrato.Cnpj);
        string? profissional = NormalizadorValores.LerTexto(contrato.Profissional);
        if (cnpj is null || profissional is null) return null;

        return $"cnpj:{cnpj}|{Texto.Normalizar(profissional)}";
    }

    private static void CopiarCampos(Contrato origem, Contrato destino)
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
        destino.Situacao = origem.Situacao;
        if (origem.Observacao is not null) destino.Observacao = origem.Observacao;
    }
}
