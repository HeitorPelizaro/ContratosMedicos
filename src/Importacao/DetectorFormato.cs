namespace ContratosMedicos.Importacao;

/// <summary>
/// Escolhe o leitor pelo CONTEÚDO do arquivo, não pela extensão — a usuária pode
/// renomear um .xlsx para .csv sem querer, e o programa não deve quebrar por isso.
/// </summary>
public static class DetectorFormato
{
    private static readonly byte[] AssinaturaZip = { 0x50, 0x4B, 0x03, 0x04 };            // xlsx/xlsm
    private static readonly byte[] AssinaturaOle2 = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }; // xls

    public static ILeitorPlanilha Escolher(string caminho)
    {
        byte[] inicio = LerInicio(caminho, 8);

        if (Comeca(inicio, AssinaturaZip)) return new LeitorXlsx();
        if (Comeca(inicio, AssinaturaOle2)) return new LeitorXls();
        if (PareceTexto(inicio)) return new LeitorCsv();

        throw new NotSupportedException(
            "Não consegui reconhecer este arquivo como planilha. " +
            "Use um arquivo do Excel (.xlsx, .xlsm ou .xls) ou um arquivo de texto (.csv).");
    }

    public static PlanilhaLida Ler(string caminho) => Escolher(caminho).Ler(caminho);

    private static byte[] LerInicio(string caminho, int quantidade)
    {
        using FileStream arquivo = File.OpenRead(caminho);
        var buffer = new byte[quantidade];
        int lidos = arquivo.Read(buffer, 0, quantidade);
        return buffer.Take(lidos).ToArray();
    }

    private static bool Comeca(byte[] conteudo, byte[] assinatura) =>
        conteudo.Length >= assinatura.Length && conteudo.Take(assinatura.Length).SequenceEqual(assinatura);

    /// <summary>Texto = sem byte de controle fora de tab/CR/LF nos primeiros bytes.</summary>
    private static bool PareceTexto(byte[] conteudo) =>
        conteudo.Length > 0 && conteudo.All(b => b >= 0x20 || b is 0x09 or 0x0A or 0x0D);
}
