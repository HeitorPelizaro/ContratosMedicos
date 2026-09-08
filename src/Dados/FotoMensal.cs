namespace ContratosMedicos.Dados;

/// <summary>
/// Foto dos totais de um mês já fechado. Existe porque o "total de médicos" de um mês
/// passado, calculado hoje, usaria o cadastro atual — e ninguém sabe quem foi cadastrado
/// depois. Uma vez fotografado, o número enviado à gerência não muda mais.
/// </summary>
public class FotoMensal
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public int MedicosDistintos { get; set; }
    public int MedicosComContratoAssinado { get; set; }
    public int ContratosTotal { get; set; }
    public int ContratosAssinadosAte { get; set; }
    public int ContratosAssinadosNoMes { get; set; }
    public int AditivosAssinadosNoMes { get; set; }
    public int MedicosDistintosNoMes { get; set; }
    public DateTime GeradoEm { get; set; } = DateTime.Now;
}
