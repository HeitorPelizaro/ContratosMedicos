using ContratosMedicos.Dominio;

namespace ContratosMedicos.Importacao.Testes;

public class ValidadorImportacaoTeste
{
    private static readonly string[] Cabecalhos = FabricaDePlanilhas.CabecalhosDaPlanilhaReal;

    private static PlanilhaLida Planilha(
        params (int Numero, string[] Celulas, string? Cor)[] linhas)
    {
        var mapaCores = new Dictionary<string, Situacao>
        {
            ["rgb:FFFF00"] = Situacao.AguardandoAssinaturaProfissional,
            ["rgb:FF0000"] = Situacao.PrazoAtrasadoSemProrrogacao,
        };
        return new PlanilhaLida(
            Cabecalhos,
            linhas.Select(l => new LinhaPlanilha(l.Numero, l.Celulas, l.Cor)).ToList(),
            mapaCores,
            Array.Empty<string>());
    }

    private static string[] Linha(
        string profissional = "Dr. A",
        string cnpj = "12.345.678/0001-99",
        string assinatura = "10/01/2026",
        string vigencia = "31/12/2026",
        string tasy = "TASY-1",
        string optante = "SIM")
        => new[] { "Regras", cnpj, optante, "Clínica X", profissional, "Cardiologia",
                   "Ambulatório", "Edital 1", tasy, "Contrato 1", assinatura, "", vigencia };

    [Fact]
    public void Converte_linha_completa_em_contrato()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        ResultadoValidacao resultado = ValidadorImportacao.Validar(Planilha((2, Linha(), "rgb:FFFF00")), mapa);

        Contrato contrato = Assert.Single(resultado.Contratos).Contrato;
        Assert.Equal("Dr. A", contrato.Profissional);
        Assert.Equal("12345678000199", contrato.Cnpj);
        Assert.True(contrato.OptanteSimplesNacional);
        Assert.Equal(new DateOnly(2026, 1, 10), contrato.DataAssinaturaOriginal);
        Assert.Equal(new DateOnly(2026, 12, 31), contrato.VigenciaFim);
        Assert.Empty(resultado.Avisos);
    }

    [Fact]
    public void Cor_da_linha_define_a_situacao()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        ResultadoValidacao resultado = ValidadorImportacao.Validar(
            Planilha((2, Linha(assinatura: ""), "rgb:FFFF00")), mapa);

        Assert.Equal(Situacao.AguardandoAssinaturaProfissional,
            Assert.Single(resultado.Contratos).Contrato.Situacao);
    }

    [Fact]
    public void Linha_sem_cor_e_sem_coluna_de_situacao_fica_nao_classificado_com_aviso()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        ResultadoValidacao resultado = ValidadorImportacao.Validar(Planilha((7, Linha(), null)), mapa);

        Assert.Equal(Situacao.NaoClassificado, Assert.Single(resultado.Contratos).Contrato.Situacao);
        Assert.Contains(resultado.Avisos, a => a.NumeroLinha == 7 && a.Mensagem.Contains("situação"));
    }

    [Fact]
    public void Data_ilegivel_gera_aviso_citando_a_linha_e_nao_descarta_o_contrato()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        ResultadoValidacao resultado = ValidadorImportacao.Validar(
            Planilha((15, Linha(vigencia: "a combinar"), "rgb:FFFF00")), mapa);

        Assert.Single(resultado.Contratos);
        Assert.Null(resultado.Contratos[0].Contrato.VigenciaFim);
        Assert.Contains(resultado.Avisos, a => a.NumeroLinha == 15 && a.Mensagem.Contains("VIGÊNCIA"));
    }

    [Fact]
    public void Cnpj_malformado_gera_aviso_mas_o_texto_e_preservado()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        ResultadoValidacao resultado = ValidadorImportacao.Validar(
            Planilha((3, Linha(cnpj: "123"), "rgb:FFFF00")), mapa);

        Assert.Equal("123", resultado.Contratos[0].Contrato.Cnpj);
        Assert.Contains(resultado.Avisos, a => a.NumeroLinha == 3 && a.Mensagem.Contains("CNPJ"));
    }

    [Fact]
    public void Codigo_tasy_repetido_entra_na_lista_de_duplicados()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        ResultadoValidacao resultado = ValidadorImportacao.Validar(Planilha(
            (2, Linha(tasy: "TASY-9"), "rgb:FFFF00"),
            (3, Linha(tasy: "TASY-9"), "rgb:FFFF00")), mapa);

        Assert.Equal("TASY-9", Assert.Single(resultado.ChavesDuplicadas));
        Assert.Equal(2, resultado.Contratos.Count); // nada é descartado em silêncio
    }

    [Fact]
    public void Linha_sem_profissional_e_sem_empresa_e_ignorada_e_contada()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        var vazia = new string[13];
        Array.Fill(vazia, "");
        vazia[12] = "31/12/2026"; // só uma data solta, sem identificação

        ResultadoValidacao resultado = ValidadorImportacao.Validar(Planilha((40, vazia, null)), mapa);

        Assert.Empty(resultado.Contratos);
        Assert.Equal(1, resultado.LinhasIgnoradas);
    }

    [Fact]
    public void Texto_do_aditivo_e_guardado_para_virar_aditivo_de_verdade()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        string[] celulas = Linha();
        celulas[11] = "T.A. de vigência 2026";

        ResultadoValidacao resultado = ValidadorImportacao.Validar(Planilha((2, celulas, "rgb:FFFF00")), mapa);

        Assert.Equal("T.A. de vigência 2026", Assert.Single(resultado.Contratos).AditivoTexto);
    }

    [Fact]
    public void Cor_escolhida_pela_usuaria_completa_o_mapa_da_legenda()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        var escolhas = new Dictionary<string, Situacao> { ["rgb:010203"] = Situacao.Duvida };

        ResultadoValidacao resultado = ValidadorImportacao.Validar(
            Planilha((2, Linha(), "rgb:010203")), mapa, escolhas);

        Assert.Equal(Situacao.Duvida, Assert.Single(resultado.Contratos).Contrato.Situacao);
    }

    [Fact]
    public void Cor_presente_na_legenda_vence_a_escolha_da_usuaria_para_a_mesma_cor()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        // rgb:FF0000 já está mapeada na legenda (ver Planilha()) para PrazoAtrasadoSemProrrogacao.
        // A usuária tenta reatribuir essa MESMA cor para outra situação — a legenda deve vencer,
        // porque "escolhas" só existe para preencher cores AUSENTES da legenda.
        var escolhas = new Dictionary<string, Situacao> { ["rgb:FF0000"] = Situacao.Duvida };

        ResultadoValidacao resultado = ValidadorImportacao.Validar(
            Planilha((2, Linha(), "rgb:FF0000")), mapa, escolhas);

        Assert.Equal(Situacao.PrazoAtrasadoSemProrrogacao,
            Assert.Single(resultado.Contratos).Contrato.Situacao);
    }

    [Fact]
    public void Cnpj_malformado_com_letra_tem_aviso_que_nao_afirma_preservacao_do_texto_original()
    {
        MapeamentoColunas mapa = MapeamentoColunas.Adivinhar(Cabecalhos);
        ResultadoValidacao resultado = ValidadorImportacao.Validar(
            Planilha((5, Linha(cnpj: "12.345-X"), "rgb:FFFF00")), mapa);

        Contrato contrato = Assert.Single(resultado.Contratos).Contrato;
        Assert.Equal("12345", contrato.Cnpj); // a letra foi descartada na normalização, valor difere do digitado

        AvisoImportacao aviso = Assert.Single(
            resultado.Avisos, a => a.NumeroLinha == 5 && a.Mensagem.Contains("CNPJ"));
        Assert.Contains("12.345-X", aviso.Mensagem); // cita o texto original digitado pela usuária
        Assert.DoesNotContain("texto foi mantido como está", aviso.Mensagem); // não pode afirmar preservação quando o valor foi alterado
    }
}
