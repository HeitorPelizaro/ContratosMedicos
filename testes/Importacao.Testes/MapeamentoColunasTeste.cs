namespace ContratosMedicos.Importacao.Testes;

public class MapeamentoColunasTeste
{
    [Fact]
    public void Adivinha_os_treze_cabecalhos_da_planilha_da_usuaria()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(FabricaDePlanilhas.CabecalhosDaPlanilhaReal);

        Assert.Equal(0, mapa.ColunaDe(CampoContrato.CadastroRegrasCriterios));
        Assert.Equal(1, mapa.ColunaDe(CampoContrato.Cnpj));
        Assert.Equal(2, mapa.ColunaDe(CampoContrato.OptanteSimplesNacional));
        Assert.Equal(3, mapa.ColunaDe(CampoContrato.Empresa));
        Assert.Equal(4, mapa.ColunaDe(CampoContrato.Profissional));
        Assert.Equal(5, mapa.ColunaDe(CampoContrato.Especialidade));
        Assert.Equal(6, mapa.ColunaDe(CampoContrato.AreaAtuacao));
        Assert.Equal(7, mapa.ColunaDe(CampoContrato.Edital));
        Assert.Equal(8, mapa.ColunaDe(CampoContrato.CodigoContratoTasy));
        Assert.Equal(9, mapa.ColunaDe(CampoContrato.ContratoOriginal));
        Assert.Equal(10, mapa.ColunaDe(CampoContrato.DataAssinaturaOriginal));
        Assert.Equal(11, mapa.ColunaDe(CampoContrato.Aditivo));
        Assert.Equal(12, mapa.ColunaDe(CampoContrato.VigenciaFim));
    }

    [Fact]
    public void Adivinha_sem_depender_de_acento_e_caixa()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(new[] { "profissional", "vigencia", "cnpj" });

        Assert.Equal(0, mapa.ColunaDe(CampoContrato.Profissional));
        Assert.Equal(1, mapa.ColunaDe(CampoContrato.VigenciaFim));
        Assert.Equal(2, mapa.ColunaDe(CampoContrato.Cnpj));
    }

    [Fact]
    public void Cabecalho_desconhecido_fica_como_ignorar()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(new[] { "PROFISSIONAL", "COLUNA QUALQUER" });

        Assert.Equal(CampoContrato.Ignorar, mapa.PorColuna[1]);
        Assert.Null(mapa.ColunaDe(CampoContrato.Empresa));
    }

    [Fact]
    public void Data_de_assinatura_nao_e_confundida_com_vigencia()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(
            new[] { "DATA DE ASSINATURA DO CONTRATO ORIGINAL", "VIGÊNCIA" });

        Assert.Equal(0, mapa.ColunaDe(CampoContrato.DataAssinaturaOriginal));
        Assert.Equal(1, mapa.ColunaDe(CampoContrato.VigenciaFim));
    }

    [Fact]
    public void Usuaria_pode_corrigir_o_mapeamento()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(new[] { "COLUNA QUALQUER" });
        mapa.Definir(0, CampoContrato.Profissional);
        Assert.Equal(0, mapa.ColunaDe(CampoContrato.Profissional));
    }

    [Fact]
    public void Um_campo_so_pode_estar_em_uma_coluna()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(new[] { "PROFISSIONAL", "OUTRA" });
        mapa.Definir(1, CampoContrato.Profissional);

        Assert.Equal(1, mapa.ColunaDe(CampoContrato.Profissional));
        Assert.Equal(CampoContrato.Ignorar, mapa.PorColuna[0]);
    }

    [Fact]
    public void Mapeamento_sobrevive_a_ida_e_volta_pela_serializacao()
    {
        MapeamentoColunas original = MapeamentoColunas.Adivinhar(FabricaDePlanilhas.CabecalhosDaPlanilhaReal);
        MapeamentoColunas voltou = MapeamentoColunas.Desserializar(original.Serializar());

        Assert.Equal(original.PorColuna, voltou.PorColuna);
    }
}
