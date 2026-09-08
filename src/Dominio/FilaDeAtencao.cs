namespace ContratosMedicos.Dominio;

public enum MotivoAtencao
{
    Vencido = 1,
    VenceEmBreve = 2,
    AditivoPendente = 3,
    Inconsistencia = 4,
    SemVigencia = 5,
}

public record ItemAtencao(Contrato Contrato, MotivoAtencao Motivo, string Explicacao, int Ordem);

/// <summary>
/// Fila de urgência: cada contrato aparece UMA vez, pelo motivo mais grave.
/// Ordem: vencido (mais antigo primeiro), vence em breve, aditivo pendente,
/// inconsistência, sem vigência.
/// </summary>
public static class FilaDeAtencao
{
    public static List<ItemAtencao> Montar(IEnumerable<Contrato> contratos, DateOnly hoje, int diasAlerta)
    {
        var itens = new List<ItemAtencao>();

        foreach (Contrato contrato in contratos)
        {
            ClassificacaoVigencia classificacao = RegrasContrato.ClassificarVigencia(contrato, hoje, diasAlerta);
            int? dias = RegrasContrato.DiasParaVencer(contrato, hoje);

            if (classificacao == ClassificacaoVigencia.Vencida)
            {
                itens.Add(new ItemAtencao(contrato, MotivoAtencao.Vencido,
                    $"Vigência vencida há {Math.Abs(dias!.Value)} dia(s).", dias.Value));
                continue;
            }

            if (classificacao == ClassificacaoVigencia.AVencer)
            {
                itens.Add(new ItemAtencao(contrato, MotivoAtencao.VenceEmBreve,
                    dias == 0 ? "A vigência vence hoje." : $"A vigência vence em {dias} dia(s).",
                    dias!.Value));
                continue;
            }

            int pendentes = contrato.Aditivos.Count(RegrasContrato.AditivoEstaPendente);
            if (pendentes > 0)
            {
                itens.Add(new ItemAtencao(contrato, MotivoAtencao.AditivoPendente,
                    $"{pendentes} aditivo(s) aguardando assinatura.", pendentes * -1));
                continue;
            }

            if (RegrasContrato.TemInconsistencia(contrato))
            {
                itens.Add(new ItemAtencao(contrato, MotivoAtencao.Inconsistencia,
                    "Tem data de assinatura, mas a situação diz que está aguardando assinatura. "
                    + "Confirme qual das duas está certa.", 0));
                continue;
            }

            if (classificacao == ClassificacaoVigencia.SemData)
                itens.Add(new ItemAtencao(contrato, MotivoAtencao.SemVigencia,
                    "Sem data de vigência: não entra nos avisos de vencimento.", 0));
        }

        return itens
            .OrderBy(i => (int)i.Motivo)
            .ThenBy(i => i.Ordem)
            .ThenBy(i => i.Contrato.Profissional, StringComparer.CurrentCulture)
            .ToList();
    }
}
