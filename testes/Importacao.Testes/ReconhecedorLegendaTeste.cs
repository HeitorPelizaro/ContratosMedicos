using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao.Testes;

public class ReconhecedorLegendaTeste
{
    [Theory]
    [InlineData("SEM CONTRATO", Situacao.SemContrato)]
    [InlineData("CONTRATO COM PRAZO ATRASADO E SEM PRORROGAÇÃO (SEM PRORROGAÇÃO CONFECCIONADO)", Situacao.PrazoAtrasadoSemProrrogacao)]
    [InlineData("AGUARDANDO ASSINATURA INSTITUCIONAL", Situacao.AguardandoAssinaturaInstitucional)]
    [InlineData("COM PROBLEMA NO CONTRATO/ADITIVO", Situacao.ProblemaNoContratoOuAditivo)]
    [InlineData("DUVIDA", Situacao.Duvida)]
    [InlineData("DÚVIDA", Situacao.Duvida)]
    [InlineData("AGUARDANDO ASSINATURA DO PROFISSIONAL", Situacao.AguardandoAssinaturaProfissional)]
    [InlineData("T.A VIGENCIA FEITO - AGUARDANDO ASSINATURA", Situacao.TaVigenciaAguardandoAssinatura)]
    [InlineData("AGUARDANDO ASSINATURA DIGITAL", Situacao.AguardandoAssinaturaDigital)]
    [InlineData("APENAS UM DOS TERMOS ADITIVOS ENVIADO", Situacao.ApenasUmTermoAditivoEnviado)]
    [InlineData("assinado", Situacao.Assinado)]
    public void Reconhece_os_nove_rotulos_da_legenda_da_planilha(string rotulo, Situacao esperada)
    {
        Assert.Equal(esperada, ReconhecedorLegenda.Reconhecer(rotulo));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("qualquer coisa que ela escreveu depois")]
    public void Rotulo_desconhecido_volta_nulo(string? rotulo)
    {
        Assert.Null(ReconhecedorLegenda.Reconhecer(rotulo));
    }
}
