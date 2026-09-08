namespace ContratosMedicos.Importacao;

public enum ModoImportacao { SubstituirTudo, AdicionarOuAtualizar }

public record ResultadoImportacao(
    int Inseridos,
    int Atualizados,
    int Removidos,
    int AditivosCriados,
    string? CaminhoBackup);
