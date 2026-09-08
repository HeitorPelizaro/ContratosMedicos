namespace ContratosMedicos.Importacao;

public interface ILeitorPlanilha
{
    bool Aceita(string caminho);
    PlanilhaLida Ler(string caminho);
}
