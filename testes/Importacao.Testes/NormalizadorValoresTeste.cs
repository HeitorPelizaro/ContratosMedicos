namespace ContratosMedicos.Importacao.Testes;

public class NormalizadorValoresTeste
{
    [Theory]
    [InlineData("31/12/2026", 2026, 12, 31)]
    [InlineData("01/01/26", 2026, 1, 1)]
    [InlineData("31-12-2026", 2026, 12, 31)]
    [InlineData("2026-12-31", 2026, 12, 31)]
    [InlineData(" 31/12/2026 ", 2026, 12, 31)]
    [InlineData("31/12/2026 (prorrogado)", 2026, 12, 31)]
    [InlineData("vigência até 31/12/2026", 2026, 12, 31)]
    [InlineData("46387", 2026, 12, 31)]   // serial do Excel
    public void Le_data_em_todos_os_formatos_que_aparecem_na_planilha(string bruto, int ano, int mes, int dia)
    {
        Assert.Equal(new DateOnly(ano, mes, dia), NormalizadorValores.LerData(bruto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("indeterminado")]
    [InlineData("a definir")]
    [InlineData("32/13/2026")]
    public void Data_que_nao_da_para_entender_volta_nula(string? bruto)
    {
        Assert.Null(NormalizadorValores.LerData(bruto));
    }

    [Fact]
    public void Data_com_periodo_pega_a_data_final()
    {
        Assert.Equal(new DateOnly(2026, 12, 31),
            NormalizadorValores.LerData("01/01/2026 a 31/12/2026"));
    }

    [Theory]
    [InlineData("12.345.678/0001-99", "12345678000199")]
    [InlineData("12345678000199", "12345678000199")]
    [InlineData(" 12.345.678/0001-99 ", "12345678000199")]
    public void Le_cnpj_deixando_so_os_digitos(string bruto, string esperado)
    {
        Assert.Equal(esperado, NormalizadorValores.LerCnpj(bruto));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    [InlineData(null)]
    public void Cnpj_curto_ou_vazio_nao_parece_valido(string? bruto)
    {
        Assert.False(NormalizadorValores.CnpjPareceValido(bruto));
    }

    [Fact]
    public void Cnpj_com_14_digitos_parece_valido()
    {
        Assert.True(NormalizadorValores.CnpjPareceValido("12.345.678/0001-99"));
    }

    [Theory]
    [InlineData("SIM", true)]
    [InlineData("sim", true)]
    [InlineData("S", true)]
    [InlineData("x", true)]
    [InlineData("optante", true)]
    [InlineData("NÃO", false)]
    [InlineData("nao", false)]
    [InlineData("N", false)]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("talvez", null)]
    public void Le_sim_nao_como_a_usuaria_escreve(string? bruto, bool? esperado)
    {
        Assert.Equal(esperado, NormalizadorValores.LerSimNao(bruto));
    }

    [Theory]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("R$ 1.234,56", 1234.56)]
    [InlineData("1234.56", 1234.56)]
    [InlineData("1234", 1234)]
    public void Le_valor_em_formato_brasileiro_e_americano(string bruto, double esperado)
    {
        Assert.Equal((decimal)esperado, NormalizadorValores.LerValor(bruto));
    }

    [Fact]
    public void Texto_vazio_vira_nulo_e_texto_com_espaco_e_aparado()
    {
        Assert.Null(NormalizadorValores.LerTexto("   "));
        Assert.Equal("Cardiologia", NormalizadorValores.LerTexto("  Cardiologia  "));
    }

    [Fact]
    public void Normalizar_tira_acento_caixa_e_espaco_repetido()
    {
        Assert.Equal("AREA DE ATUACAO", Texto.Normalizar("  Área   de   Atuação "));
    }

    [Fact]
    public void Data_iso_em_texto_ruidoso_parseia_corretamente()
    {
        // Regression: DataSolta regex was matching "26-12-31" from "2026-12-31"
        // and parsing as 2031-12-26 instead of returning correct date
        Assert.Equal(new DateOnly(2026, 12, 31),
            NormalizadorValores.LerData("vigência 2026-12-31 conforme aditivo"));
    }
}
