namespace ContratosMedicos.Importacao;

public enum CampoContrato
{
    Ignorar = 0,
    CadastroRegrasCriterios,
    Cnpj,
    OptanteSimplesNacional,
    Empresa,
    Profissional,
    Especialidade,
    AreaAtuacao,
    Edital,
    CodigoContratoTasy,
    ContratoOriginal,
    DataAssinaturaOriginal,
    VigenciaFim,
    Aditivo,
    SituacaoTexto,
    Observacao,
}

public static class RotulosCampo
{
    private static readonly Dictionary<CampoContrato, string> Mapa = new()
    {
        [CampoContrato.Ignorar] = "Não importar esta coluna",
        [CampoContrato.CadastroRegrasCriterios] = "Cadastro regras e critérios / repasse honorários médicos",
        [CampoContrato.Cnpj] = "CNPJ",
        [CampoContrato.OptanteSimplesNacional] = "Optante Simples Nacional",
        [CampoContrato.Empresa] = "Empresa",
        [CampoContrato.Profissional] = "Profissional",
        [CampoContrato.Especialidade] = "Especialidade",
        [CampoContrato.AreaAtuacao] = "Área de atuação",
        [CampoContrato.Edital] = "Edital",
        [CampoContrato.CodigoContratoTasy] = "Código contrato Tasy",
        [CampoContrato.ContratoOriginal] = "Contrato original",
        [CampoContrato.DataAssinaturaOriginal] = "Data de assinatura do contrato original",
        [CampoContrato.VigenciaFim] = "Vigência (data final)",
        [CampoContrato.Aditivo] = "Aditivo",
        [CampoContrato.SituacaoTexto] = "Situação (em texto)",
        [CampoContrato.Observacao] = "Observação",
    };

    public static string Rotulo(this CampoContrato campo) => Mapa[campo];
}
