namespace ContratosMedicos.Dominio.Testes;

public class DescricoesSituacaoTeste
{
    [Fact]
    public void Toda_situacao_tem_rotulo_e_cor()
    {
        foreach (Situacao situacao in Enum.GetValues<Situacao>())
        {
            Assert.False(string.IsNullOrWhiteSpace(DescricoesSituacao.Rotulo(situacao)));
            Assert.Matches("^#[0-9A-Fa-f]{6}$", DescricoesSituacao.Cor(situacao));
        }
    }

    [Fact]
    public void Rotulos_usam_o_texto_da_legenda_da_planilha()
    {
        Assert.Equal("Aguardando assinatura do profissional",
            DescricoesSituacao.Rotulo(Situacao.AguardandoAssinaturaProfissional));
        Assert.Equal("Sem contrato", DescricoesSituacao.Rotulo(Situacao.SemContrato));
    }

    [Fact]
    public void Todas_lista_as_situacoes_com_assinado_primeiro()
    {
        Assert.Equal(Situacao.Assinado, DescricoesSituacao.Todas[0]);
        Assert.Equal(Enum.GetValues<Situacao>().Length, DescricoesSituacao.Todas.Count);
    }
}
