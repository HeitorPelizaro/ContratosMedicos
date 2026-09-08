using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao;

public record AvisoImportacao(int NumeroLinha, string Mensagem);

/// <param name="AditivoTexto">Conteúdo da coluna ADITIVO da planilha, que vira um aditivo real na gravação.</param>
public record ContratoLido(Contrato Contrato, string? AditivoTexto);

public record ResultadoValidacao(
    IReadOnlyList<ContratoLido> Contratos,
    IReadOnlyList<AvisoImportacao> Avisos,
    int LinhasIgnoradas,
    IReadOnlyList<string> ChavesDuplicadas);
