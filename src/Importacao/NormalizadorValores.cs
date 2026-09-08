using System.Globalization;
using System.Text.RegularExpressions;

namespace ContratosMedicos.Importacao;

/// <summary>
/// Converte o que está escrito na planilha em valor de domínio. Tolerante por projeto:
/// o que não dá para entender volta nulo e vira aviso no relatório de validação —
/// nunca exceção, nunca linha descartada em silêncio.
/// </summary>
public static class NormalizadorValores
{
    private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly string[] FormatosData =
    {
        "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "d/M/yy",
        "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "dd.MM.yyyy",
    };

    private static readonly Regex DataSolta = new(@"(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{2,4})", RegexOptions.Compiled);
    private static readonly Regex DataIso = new(@"\b(\d{4})-(\d{1,2})-(\d{1,2})\b", RegexOptions.Compiled);
    private static readonly Regex SomenteDigitos = new(@"\D", RegexOptions.Compiled);

    public static string? LerTexto(string? bruto) =>
        string.IsNullOrWhiteSpace(bruto) ? null : bruto.Trim();

    public static DateOnly? LerData(string? bruto)
    {
        if (string.IsNullOrWhiteSpace(bruto)) return null;
        string valor = bruto.Trim();

        if (DateOnly.TryParseExact(valor, FormatosData, Brasil, DateTimeStyles.None, out DateOnly exata))
            return exata;

        // Serial do Excel: dias desde 30/12/1899. Faixa aceita: 1990..2100.
        if (double.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out double serial)
            && serial >= 32874 && serial <= 73415)
            return DateOnly.FromDateTime(new DateTime(1899, 12, 30).AddDays(serial));

        // Texto com sujeira ou período: vale a ÚLTIMA data encontrada (data final).
        // Primeiro tenta formato ISO (yyyy-MM-dd), que é inequívoco com 4 dígitos no ano.
        MatchCollection achadsIso = DataIso.Matches(valor);
        for (int i = achadsIso.Count - 1; i >= 0; i--)
        {
            Match m = achadsIso[i];
            if (int.TryParse(m.Groups[1].Value, out int ano) &&
                int.TryParse(m.Groups[2].Value, out int mes) &&
                int.TryParse(m.Groups[3].Value, out int dia))
            {
                try
                {
                    return new DateOnly(ano, mes, dia);
                }
                catch
                {
                    // Data inválida (ex: 2026-02-30), continua buscando
                }
            }
        }

        // Depois tenta formato de barra/travessão/ponto (dd/MM/yyyy, etc).
        MatchCollection achados = DataSolta.Matches(valor);
        for (int i = achados.Count - 1; i >= 0; i--)
        {
            if (DateOnly.TryParseExact(achados[i].Value.Replace('.', '/').Replace('-', '/'),
                    FormatosData, Brasil, DateTimeStyles.None, out DateOnly doTexto))
                return doTexto;
        }

        return null;
    }

    public static string? LerCnpj(string? bruto)
    {
        if (string.IsNullOrWhiteSpace(bruto)) return null;
        string digitos = SomenteDigitos.Replace(bruto, "");
        return digitos.Length == 0 ? null : digitos;
    }

    public static bool CnpjPareceValido(string? bruto) => LerCnpj(bruto)?.Length == 14;

    public static bool? LerSimNao(string? bruto)
    {
        string valor = Texto.Normalizar(bruto);
        if (valor.Length == 0) return null;
        if (valor is "SIM" or "S" or "X" or "OPTANTE" or "TRUE" or "1" or "VERDADEIRO") return true;
        if (valor is "NAO" or "N" or "FALSE" or "0" or "FALSO" or "NAO OPTANTE") return false;
        return null;
    }

    public static decimal? LerValor(string? bruto)
    {
        if (string.IsNullOrWhiteSpace(bruto)) return null;
        string valor = bruto.Replace("R$", "").Trim();

        // Se tem vírgula, é formato brasileiro
        if (valor.Contains(','))
        {
            if (decimal.TryParse(valor, NumberStyles.Currency, Brasil, out decimal brasileiro))
                return brasileiro;
        }
        // Caso contrário, tenta formato americano (ou inteiro)
        if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal americano))
            return americano;
        return null;
    }
}
