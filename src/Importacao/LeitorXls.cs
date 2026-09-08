using System.Data;
using System.Text;
using ContratosMedicos.Dominio;
using ExcelDataReader;

namespace ContratosMedicos.Importacao;

/// <summary>
/// Excel 97-2003 (.xls). Formato binário antigo: dá para ler os dados, mas NÃO as cores —
/// as linhas vêm sem situação e caem como "não classificado", o que o relatório de validação avisa.
/// </summary>
public class LeitorXls : ILeitorPlanilha
{
    static LeitorXls() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public bool Aceita(string caminho) =>
        Path.GetExtension(caminho).ToLowerInvariant() == ".xls";

    public PlanilhaLida Ler(string caminho)
    {
        using FileStream arquivo = File.OpenRead(caminho);
        using IExcelDataReader leitor = ExcelReaderFactory.CreateReader(arquivo);
        DataSet conjunto = leitor.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false },
        });

        DataTable tabela = EscolherTabela(conjunto);
        var cabecalhos = new List<string>();
        var linhas = new List<LinhaPlanilha>();

        for (int r = 0; r < tabela.Rows.Count; r++)
        {
            List<string> celulas = tabela.Columns.Cast<DataColumn>()
                .Select(c => tabela.Rows[r][c]?.ToString()?.Trim() ?? "")
                .ToList();

            if (celulas.All(string.IsNullOrWhiteSpace)) continue;

            if (cabecalhos.Count == 0)
            {
                cabecalhos.AddRange(celulas);
                continue;
            }

            linhas.Add(new LinhaPlanilha(r + 1, celulas, null));
        }

        return new PlanilhaLida(cabecalhos, linhas, new Dictionary<string, Situacao>(), Array.Empty<string>());
    }

    private static DataTable EscolherTabela(DataSet conjunto)
    {
        foreach (DataTable tabela in conjunto.Tables)
            if (Texto.Normalizar(tabela.TableName).Contains("GERAL", StringComparison.Ordinal))
                return tabela;

        foreach (DataTable tabela in conjunto.Tables)
            if (!Texto.Normalizar(tabela.TableName).Contains("LEGENDA", StringComparison.Ordinal))
                return tabela;

        return conjunto.Tables[0];
    }
}
