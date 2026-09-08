using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao;

/// <summary>
/// Transforma a planilha lida em contratos, acumulando avisos. Regra de ouro:
/// nada é descartado em silêncio e nada é bloqueante — o que não deu para entender
/// vira aviso e aparece na tela Atenção depois de importado.
/// </summary>
public static class ValidadorImportacao
{
    public static ResultadoValidacao Validar(
        PlanilhaLida planilha,
        MapeamentoColunas mapeamento,
        IReadOnlyDictionary<string, Situacao>? coresEscolhidasPelaUsuaria = null)
    {
        var contratos = new List<ContratoLido>();
        var avisos = new List<AvisoImportacao>();
        var vistos = new Dictionary<string, int>();
        var duplicadas = new List<string>();
        int ignoradas = 0;

        foreach (LinhaPlanilha linha in planilha.Linhas)
        {
            string? Valor(CampoContrato campo)
            {
                int? coluna = mapeamento.ColunaDe(campo);
                if (coluna is null || coluna >= linha.Celulas.Count) return null;
                return NormalizadorValores.LerTexto(linha.Celulas[coluna.Value]);
            }

            string? profissional = Valor(CampoContrato.Profissional);
            string? empresa = Valor(CampoContrato.Empresa);
            string? tasy = Valor(CampoContrato.CodigoContratoTasy);

            if (profissional is null && empresa is null && tasy is null)
            {
                ignoradas++;
                continue;
            }

            string? cnpjBruto = Valor(CampoContrato.Cnpj);
            string? cnpj = NormalizadorValores.LerCnpj(cnpjBruto) ?? cnpjBruto;

            var contrato = new Contrato
            {
                CadastroRegrasCriterios = Valor(CampoContrato.CadastroRegrasCriterios),
                Cnpj = cnpj,
                OptanteSimplesNacional = NormalizadorValores.LerSimNao(Valor(CampoContrato.OptanteSimplesNacional)),
                Empresa = empresa,
                Profissional = profissional,
                Especialidade = Valor(CampoContrato.Especialidade),
                AreaAtuacao = Valor(CampoContrato.AreaAtuacao),
                Edital = Valor(CampoContrato.Edital),
                CodigoContratoTasy = tasy,
                ContratoOriginal = Valor(CampoContrato.ContratoOriginal),
                Observacao = Valor(CampoContrato.Observacao),
            };

            contrato.DataAssinaturaOriginal = LerData(
                Valor(CampoContrato.DataAssinaturaOriginal), linha.Numero,
                "DATA DE ASSINATURA DO CONTRATO ORIGINAL", avisos);

            contrato.VigenciaFim = LerData(
                Valor(CampoContrato.VigenciaFim), linha.Numero, "VIGÊNCIA", avisos);

            if (contrato.Cnpj is not null && !NormalizadorValores.CnpjPareceValido(contrato.Cnpj))
                avisos.Add(new AvisoImportacao(linha.Numero,
                    $"CNPJ \"{cnpjBruto}\" não tem 14 dígitos. Os dígitos foram mantidos como \"{contrato.Cnpj}\"."));

            contrato.Situacao = DefinirSituacao(planilha, linha, mapeamento, coresEscolhidasPelaUsuaria);
            if (contrato.Situacao == Situacao.NaoClassificado)
                avisos.Add(new AvisoImportacao(linha.Numero,
                    "Não foi possível descobrir a situação (sem cor na linha e sem coluna de situação). "
                    + "Ficou como \"Não classificado\"."));

            string? chave = tasy ?? (contrato.Cnpj is null || profissional is null
                ? null
                : $"{contrato.Cnpj}|{Texto.Normalizar(profissional)}");

            if (chave is not null)
            {
                if (vistos.TryGetValue(chave, out int linhaAnterior))
                {
                    if (!duplicadas.Contains(chave)) duplicadas.Add(chave);
                    avisos.Add(new AvisoImportacao(linha.Numero,
                        $"Este contrato já aparece na linha {linhaAnterior} (mesma identificação \"{chave}\")."));
                }
                else vistos[chave] = linha.Numero;
            }

            contratos.Add(new ContratoLido(contrato, Valor(CampoContrato.Aditivo)));
        }

        return new ResultadoValidacao(contratos, avisos, ignoradas, duplicadas);
    }

    private static DateOnly? LerData(string? bruto, int numeroLinha, string nomeColuna, List<AvisoImportacao> avisos)
    {
        if (bruto is null) return null;
        DateOnly? data = NormalizadorValores.LerData(bruto);
        if (data is null)
            avisos.Add(new AvisoImportacao(numeroLinha,
                $"Não entendi a data \"{bruto}\" na coluna {nomeColuna}. O campo ficou vazio."));
        return data;
    }

    private static Situacao DefinirSituacao(
        PlanilhaLida planilha,
        LinhaPlanilha linha,
        MapeamentoColunas mapeamento,
        IReadOnlyDictionary<string, Situacao>? escolhas)
    {
        int? colunaSituacao = mapeamento.ColunaDe(CampoContrato.SituacaoTexto);
        if (colunaSituacao is not null && colunaSituacao < linha.Celulas.Count)
        {
            Situacao? pelaColuna = ReconhecedorLegenda.Reconhecer(linha.Celulas[colunaSituacao.Value]);
            if (pelaColuna is not null) return pelaColuna.Value;
        }

        if (linha.ChaveCor is not null)
        {
            if (planilha.MapaCores.TryGetValue(linha.ChaveCor, out Situacao pelaCor))
                return pelaCor;
            if (escolhas is not null && escolhas.TryGetValue(linha.ChaveCor, out Situacao escolhida))
                return escolhida;
        }

        return Situacao.NaoClassificado;
    }
}
