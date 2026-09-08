using System.Globalization;
using System.Text;

namespace ContratosMedicos.Importacao;

public static class Texto
{
    public static string SemAcento(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return "";
        string decomposto = valor.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);
        foreach (char c in decomposto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                construtor.Append(c);
        return construtor.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Maiúsculas, sem acento, espaços colapsados. Usado para comparar cabeçalhos e rótulos.</summary>
    public static string Normalizar(string? valor)
    {
        string sem = SemAcento(valor).ToUpperInvariant().Trim();
        return string.Join(' ', sem.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
