using System.Diagnostics;

namespace ContratosMedicos.App;

/// <summary>
/// Abre a interface numa janela de navegador em modo aplicativo (sem barra de endereço),
/// para parecer um programa e não um site. Ordem: Edge, Chrome, navegador padrão.
/// </summary>
public static class AbridorNavegador
{
    private static readonly string[] CaminhosEdgeEChrome =
    {
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
    };

    public static void Abrir(string url)
    {
        foreach (string navegador in CaminhosEdgeEChrome)
        {
            if (!File.Exists(navegador)) continue;
            try
            {
                Process.Start(new ProcessStartInfo(navegador,
                    $"--app={url} --window-size=1400,900 --disable-features=Translate"));
                return;
            }
            catch (Exception) { /* tenta o próximo */ }
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Sem navegador: o log já registrou a URL, e o programa continua servindo.
        }
    }
}
