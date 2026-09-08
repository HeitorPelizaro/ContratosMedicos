using System.Text;

namespace ContratosMedicos.Importacao;

/// <summary>
/// CSV brasileiro: separador pode ser ; ou , e a codificação pode ser UTF-8 ou Windows-1252
/// (acento estragado é o defeito clássico). Ambos são detectados pelo conteúdo.
/// </summary>
public class LeitorCsv : ILeitorPlanilha
{
    static LeitorCsv() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public bool Aceita(string caminho) =>
        Path.GetExtension(caminho).ToLowerInvariant() is ".csv" or ".txt";

    public PlanilhaLida Ler(string caminho)
    {
        byte[] bytes = File.ReadAllBytes(caminho);
        string conteudo = Decodificar(bytes);
        string[] linhasBrutas = conteudo.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        char separador = DetectarSeparador(linhasBrutas);

        var cabecalhos = new List<string>();
        var linhas = new List<LinhaPlanilha>();

        for (int i = 0; i < linhasBrutas.Length; i++)
        {
            string bruta = linhasBrutas[i];
            if (string.IsNullOrWhiteSpace(bruta)) continue;

            List<string> celulas = DividirLinha(bruta, separador);

            if (cabecalhos.Count == 0)
            {
                cabecalhos.AddRange(celulas.Select(c => c.Trim()));
                continue;
            }

            linhas.Add(new LinhaPlanilha(i + 1, celulas, null));
        }

        return new PlanilhaLida(cabecalhos, linhas,
            new Dictionary<string, ContratosMedicos.Dominio.Situacao>(), Array.Empty<string>());
    }

    /// <summary>UTF-8 se o texto for UTF-8 válido; senão Windows-1252.</summary>
    private static string Decodificar(byte[] bytes)
    {
        try
        {
            var estrito = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return estrito.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1252).GetString(bytes);
        }
    }

    /// <summary>Ganha o separador que aparece mais vezes na primeira linha não vazia.</summary>
    private static char DetectarSeparador(IEnumerable<string> linhas)
    {
        string? primeira = linhas.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
        if (primeira is null) return ';';
        return primeira.Count(c => c == ';') >= primeira.Count(c => c == ',') ? ';' : ',';
    }

    /// <summary>Divisão que respeita campo entre aspas, inclusive aspas dobradas.</summary>
    private static List<string> DividirLinha(string linha, char separador)
    {
        var celulas = new List<string>();
        var atual = new StringBuilder();
        bool dentroDeAspas = false;

        for (int i = 0; i < linha.Length; i++)
        {
            char c = linha[i];
            if (c == '"')
            {
                if (dentroDeAspas && i + 1 < linha.Length && linha[i + 1] == '"')
                {
                    atual.Append('"');
                    i++;
                }
                else dentroDeAspas = !dentroDeAspas;
            }
            else if (c == separador && !dentroDeAspas)
            {
                celulas.Add(atual.ToString().Trim());
                atual.Clear();
            }
            else atual.Append(c);
        }

        celulas.Add(atual.ToString().Trim());
        return celulas;
    }
}
