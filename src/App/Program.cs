using System.Globalization;
using System.Net;
using System.Net.Sockets;
using ContratosMedicos.App;
using ContratosMedicos.App.Components;
using ContratosMedicos.Dados;
using ContratosMedicos.Importacao;
using Microsoft.EntityFrameworkCore;

// Cultura brasileira em todo o app: datas dd/MM/yyyy e decimal com vírgula.
var brasil = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = brasil;
CultureInfo.DefaultThreadCurrentUICulture = brasil;

var caminhos = new CaminhosApp();
caminhos.GarantirPastas();

// Instância única: se já existe um processo servindo, apenas reabre a janela dele.
using var trava = new Mutex(initiallyOwned: true, "ContratosMedicos.InstanciaUnica", out bool primeira);
if (!primeira)
{
    string? urlAnterior = LerUrlPublicada(caminhos);
    if (urlAnterior is not null) AbridorNavegador.Abrir(urlAnterior);
    return;
}

int porta = PortaLivre();
var estado = new EstadoDaSessao();

var construtor = WebApplication.CreateBuilder(args);
construtor.WebHost.UseUrls($"http://127.0.0.1:{porta}");
construtor.Logging.AddFile(Path.Combine(caminhos.PastaLog, "app-{Date}.log"));

construtor.Services.AddSingleton(caminhos);
construtor.Services.AddSingleton(estado);
construtor.Services.AddSingleton(new ServicoBackup(caminhos));
// Fábrica, não contexto injetado: no Blazor interativo por servidor o escopo de injeção é
// o circuito (a sessão inteira), então um DbContext Scoped viveria horas acumulando alterações
// não salvas — uma edição cancelada seria gravada pelo próximo SaveChanges de qualquer tela.
// Com a fábrica, cada operação abre e fecha o seu contexto.
construtor.Services.AddDbContextFactory<ContratosDbContext>(
    o => o.UseSqlite(caminhos.StringDeConexao));
construtor.Services.AddScoped<RepositorioContratos>();
construtor.Services.AddScoped<RepositorioIndicadores>();
construtor.Services.AddScoped<ServicoConfiguracoes>();
construtor.Services.AddScoped<ServicoImportacao>();
construtor.Services.AddScoped<ServicoAnexos>();

// Template unificado do .NET 8 (não o antigo Blazor Server): componentes Razor
// hospedados com renderização interativa via SignalR, equivalente ao AddServerSideBlazor de antes.
construtor.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// AddInteractiveServerComponents() só configura opções de componente, não o tamanho de
// mensagem do hub SignalR — o limite de upload padrão (32 KB) é pequeno demais para as
// planilhas de até 60 MB da tela de importação, então configuramos o hub à parte.
construtor.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(
    o => o.MaximumReceiveMessageSize = 64 * 1024 * 1024);

WebApplication app = construtor.Build();

using (IServiceScope escopo = app.Services.CreateScope())
{
    var fabrica = escopo.ServiceProvider
        .GetRequiredService<IDbContextFactory<ContratosDbContext>>();
    using (ContratosDbContext banco = fabrica.CreateDbContext())
        banco.Database.EnsureCreated();

    escopo.ServiceProvider.GetRequiredService<ServicoBackup>()
        .CriarBackupSeNecessario(DateOnly.FromDateTime(DateTime.Now));
}

app.UseStaticFiles();

// Só quem chegou pela URL com o token entra. Impede que outro programa rodando
// na mesma máquina fale com o app pela porta de loopback.
app.Use(async (contexto, seguir) =>
{
    const string nomeCookie = "cm-sessao";

    if (contexto.Request.Query["t"] == estado.Token)
    {
        contexto.Response.Cookies.Append(nomeCookie, estado.Token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
        });
        await seguir();
        return;
    }

    if (contexto.Request.Cookies[nomeCookie] == estado.Token)
    {
        await seguir();
        return;
    }

    contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
    await contexto.Response.WriteAsync("Abra o programa pelo atalho na área de trabalho.");
});

app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

estado.UrlBase = $"http://127.0.0.1:{porta}";
string url = $"{estado.UrlBase}/?t={estado.Token}";
PublicarUrl(caminhos, url);
app.Logger.LogInformation("Interface disponível em {Url}", url);

app.Start();
AbridorNavegador.Abrir(url);
app.WaitForShutdown();
return;

static int PortaLivre()
{
    var ouvinte = new TcpListener(IPAddress.Loopback, 0);
    ouvinte.Start();
    int porta = ((IPEndPoint)ouvinte.LocalEndpoint).Port;
    ouvinte.Stop();
    return porta;
}

static void PublicarUrl(CaminhosApp caminhos, string url) =>
    File.WriteAllText(Path.Combine(caminhos.PastaDados, "sessao-atual.txt"), url);

static string? LerUrlPublicada(CaminhosApp caminhos)
{
    string arquivo = Path.Combine(caminhos.PastaDados, "sessao-atual.txt");
    return File.Exists(arquivo) ? File.ReadAllText(arquivo).Trim() : null;
}
