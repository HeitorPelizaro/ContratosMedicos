using Microsoft.EntityFrameworkCore;

namespace ContratosMedicos.Dados;

public class ServicoConfiguracoes
{
    public const string ChaveDiasAlerta = "dias-alerta-vencimento";
    private readonly IDbContextFactory<ContratosDbContext> _fabrica;

    public ServicoConfiguracoes(IDbContextFactory<ContratosDbContext> fabrica) => _fabrica = fabrica;

    public int DiasAlerta =>
        int.TryParse(Obter(ChaveDiasAlerta), out int dias) ? dias : 30;

    public void DefinirDiasAlerta(int dias)
    {
        if (dias < 1 || dias > 365)
            throw new ArgumentOutOfRangeException(nameof(dias), "O aviso de vencimento deve ficar entre 1 e 365 dias.");
        Definir(ChaveDiasAlerta, dias.ToString());
    }

    public string? Obter(string chave)
    {
        using ContratosDbContext banco = _fabrica.CreateDbContext();
        return banco.Configuracoes.AsNoTracking().FirstOrDefault(c => c.Chave == chave)?.Valor;
    }

    public void Definir(string chave, string valor)
    {
        using ContratosDbContext banco = _fabrica.CreateDbContext();

        Configuracao? existente = banco.Configuracoes.FirstOrDefault(c => c.Chave == chave);
        if (existente is null)
            banco.Configuracoes.Add(new Configuracao { Chave = chave, Valor = valor });
        else
            existente.Valor = valor;
        banco.SaveChanges();
    }
}
