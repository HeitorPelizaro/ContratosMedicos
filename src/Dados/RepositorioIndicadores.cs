using ContratosMedicos.Dominio;
using Microsoft.EntityFrameworkCore;

namespace ContratosMedicos.Dados;

/// <param name="VeioDeFoto">true quando os totais são a foto gravada daquele mês.</param>
public record IndicadorApresentado(IndicadoresMes Indicadores, bool VeioDeFoto, DateTime? FotografadoEm);

public class RepositorioIndicadores
{
    private readonly IDbContextFactory<ContratosDbContext> _fabrica;
    private readonly RepositorioContratos _contratos;

    public RepositorioIndicadores(IDbContextFactory<ContratosDbContext> fabrica, RepositorioContratos contratos)
    {
        _fabrica = fabrica;
        _contratos = contratos;
    }

    /// <summary>
    /// Mês fechado: usa a foto; se não existe, calcula e fotografa na hora.
    /// Mês corrente ou futuro: calcula sempre, sem fotografar — ainda vai mudar.
    ///
    /// Exceção importante: mês fechado que calcula zero contrato NÃO é fotografado. Abrir esta
    /// tela antes da primeira importação congelaria o mês passado em zero para sempre, e a
    /// usuária não tem como desfazer isso. Sem dado, segue recalculando.
    /// </summary>
    public IndicadorApresentado Obter(int ano, int mes, DateOnly hoje)
    {
        IndicadoresMes calculado = CalculadoraIndicadores.Calcular(_contratos.Todos(), ano, mes);
        bool mesFechado = calculado.Corte < new DateOnly(hoje.Year, hoje.Month, 1);

        if (!mesFechado) return new IndicadorApresentado(calculado, false, null);

        using ContratosDbContext banco = _fabrica.CreateDbContext();
        FotoMensal? foto = banco.FotosMensais.AsNoTracking()
            .FirstOrDefault(f => f.Ano == ano && f.Mes == mes);

        if (foto is null)
        {
            if (calculado.ContratosTotal == 0) return new IndicadorApresentado(calculado, false, null);

            Fotografar(calculado);
            foto = banco.FotosMensais.AsNoTracking().First(f => f.Ano == ano && f.Mes == mes);
        }

        // Os totais vêm da foto; a quebra por especialidade é sempre a atual (a foto não a guarda).
        IndicadoresMes daFoto = calculado with
        {
            MedicosDistintos = foto.MedicosDistintos,
            MedicosComContratoAssinado = foto.MedicosComContratoAssinado,
            ContratosTotal = foto.ContratosTotal,
            ContratosAssinadosAte = foto.ContratosAssinadosAte,
            ContratosAssinadosNoMes = foto.ContratosAssinadosNoMes,
            AditivosAssinadosNoMes = foto.AditivosAssinadosNoMes,
            MedicosDistintosNoMes = foto.MedicosDistintosNoMes,
        };

        return new IndicadorApresentado(daFoto, true, foto.GeradoEm);
    }

    /// <summary>
    /// Refaz a foto do mês com o cadastro de agora, sobrescrevendo a que existia.
    /// A tela pede confirmação antes: o número pode já ter ido para a gerência.
    /// </summary>
    public IndicadoresMes RefazerFoto(int ano, int mes)
    {
        IndicadoresMes calculado = CalculadoraIndicadores.Calcular(_contratos.Todos(), ano, mes);
        Fotografar(calculado);
        return calculado;
    }

    public void Fotografar(IndicadoresMes i)
    {
        using ContratosDbContext banco = _fabrica.CreateDbContext();

        FotoMensal? existente = banco.FotosMensais
            .FirstOrDefault(f => f.Ano == i.Ano && f.Mes == i.Mes);

        FotoMensal foto = existente ?? new FotoMensal { Ano = i.Ano, Mes = i.Mes };

        foto.MedicosDistintos = i.MedicosDistintos;
        foto.MedicosComContratoAssinado = i.MedicosComContratoAssinado;
        foto.ContratosTotal = i.ContratosTotal;
        foto.ContratosAssinadosAte = i.ContratosAssinadosAte;
        foto.ContratosAssinadosNoMes = i.ContratosAssinadosNoMes;
        foto.AditivosAssinadosNoMes = i.AditivosAssinadosNoMes;
        foto.MedicosDistintosNoMes = i.MedicosDistintosNoMes;
        foto.GeradoEm = DateTime.Now;

        if (existente is null) banco.FotosMensais.Add(foto);
        banco.SaveChanges();
    }
}
