namespace ContratosMedicos.Importacao;

/// <summary>
/// Qual coluna da planilha alimenta qual campo. A adivinhação usa o cabeçalho normalizado;
/// a usuária pode corrigir qualquer par na tela de importação, e o mapeamento fica salvo.
/// </summary>
public class MapeamentoColunas
{
    /// <summary>Padrões em ordem: o primeiro que casar ganha. Os mais específicos vêm antes.</summary>
    private static readonly (string Padrao, CampoContrato Campo)[] Padroes =
    {
        ("DATA DE ASSINATURA", CampoContrato.DataAssinaturaOriginal),
        ("CODIGO CONTRATO TASY", CampoContrato.CodigoContratoTasy),
        ("CODIGO TASY", CampoContrato.CodigoContratoTasy),
        ("CONTRATO ORIGINAL", CampoContrato.ContratoOriginal),
        ("CADASTRO REGRAS", CampoContrato.CadastroRegrasCriterios),
        ("OPTANTE", CampoContrato.OptanteSimplesNacional),
        ("SIMPLES NACIONAL", CampoContrato.OptanteSimplesNacional),
        ("AREA DE ATUACAO", CampoContrato.AreaAtuacao),
        ("ESPECIALIDADE", CampoContrato.Especialidade),
        ("PROFISSIONAL", CampoContrato.Profissional),
        ("EMPRESA", CampoContrato.Empresa),
        ("EDITAL", CampoContrato.Edital),
        ("CNPJ", CampoContrato.Cnpj),
        ("VIGENCIA", CampoContrato.VigenciaFim),
        ("ADITIVO", CampoContrato.Aditivo),
        ("SITUACAO", CampoContrato.SituacaoTexto),
        ("STATUS", CampoContrato.SituacaoTexto),
        ("OBSERVACAO", CampoContrato.Observacao),
        ("OBS", CampoContrato.Observacao),
    };

    private readonly Dictionary<int, CampoContrato> _porColuna = new();

    public IReadOnlyDictionary<int, CampoContrato> PorColuna => _porColuna;

    public static MapeamentoColunas Adivinhar(IReadOnlyList<string> cabecalhos)
    {
        var mapeamento = new MapeamentoColunas();

        for (int i = 0; i < cabecalhos.Count; i++)
            mapeamento._porColuna[i] = CampoContrato.Ignorar;

        for (int i = 0; i < cabecalhos.Count; i++)
        {
            string normalizado = Texto.Normalizar(cabecalhos[i]);
            foreach ((string padrao, CampoContrato campo) in Padroes)
            {
                if (!normalizado.Contains(padrao, StringComparison.Ordinal)) continue;
                if (mapeamento.ColunaDe(campo) is not null) continue; // primeira coluna vence
                mapeamento._porColuna[i] = campo;
                break;
            }
        }

        return mapeamento;
    }

    public void Definir(int coluna, CampoContrato campo)
    {
        if (campo != CampoContrato.Ignorar)
            foreach (int outra in _porColuna.Where(p => p.Value == campo && p.Key != coluna)
                                            .Select(p => p.Key).ToList())
                _porColuna[outra] = CampoContrato.Ignorar;

        _porColuna[coluna] = campo;
    }

    public int? ColunaDe(CampoContrato campo)
    {
        foreach ((int coluna, CampoContrato atual) in _porColuna)
            if (atual == campo) return coluna;
        return null;
    }

    public string Serializar() =>
        string.Join(';', _porColuna.OrderBy(p => p.Key).Select(p => $"{p.Key}={(int)p.Value}"));

    public static MapeamentoColunas Desserializar(string texto)
    {
        var mapeamento = new MapeamentoColunas();
        foreach (string par in texto.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] partes = par.Split('=');
            if (partes.Length == 2
                && int.TryParse(partes[0], out int coluna)
                && int.TryParse(partes[1], out int campo))
                mapeamento._porColuna[coluna] = (CampoContrato)campo;
        }
        return mapeamento;
    }
}
