using System.Text;

namespace ContratosMedicos.Dominio;

/// <summary>
/// Texto pronto para colar no e-mail da gerência. Sem markdown, sem tabela HTML:
/// texto simples alinhado, que sobrevive a copiar e colar no Outlook.
/// </summary>
public static class TextoDoIndicador
{
    public static string ParaEmail(IndicadoresMes i)
    {
        var texto = new StringBuilder();

        texto.AppendLine($"Indicador de contratos médicos — {i.NomeDoMes}");
        texto.AppendLine($"(posição em {i.Corte:dd/MM/yyyy})");
        texto.AppendLine();
        texto.AppendLine($"Médicos: {i.MedicosDistintos}");
        texto.AppendLine($"Contratos cadastrados: {i.ContratosTotal}");
        texto.AppendLine($"Contratos assinados: {i.ContratosAssinadosAte}");
        texto.AppendLine($"Médicos com contrato assinado: {i.MedicosComContratoAssinado}");
        texto.AppendLine($"Cobertura: {Percentual(i.PercentualCobertura)}");
        texto.AppendLine();
        texto.AppendLine("No mês:");
        texto.AppendLine($"  Contratos assinados no mês: {i.ContratosAssinadosNoMes}");
        texto.AppendLine($"  Médicos envolvidos: {i.MedicosDistintosNoMes}");
        texto.AppendLine($"  Aditivos assinados no mês: {i.AditivosAssinadosNoMes}");

        if (i.PorEspecialidade.Count > 0)
        {
            texto.AppendLine();
            texto.AppendLine("Por especialidade (médicos / contratos / assinados):");
            int largura = i.PorEspecialidade.Max(l => l.Rotulo.Length);
            foreach (LinhaIndicador linha in i.PorEspecialidade)
                texto.AppendLine($"  {linha.Rotulo.PadRight(largura)}  "
                                 + $"{linha.Medicos} / {linha.Contratos} / {linha.ContratosAssinados}");
        }

        return texto.ToString();
    }

    private static string Percentual(decimal? valor) =>
        valor is null ? "—" : $"{valor.Value:0.0}%".Replace('.', ',');
}
