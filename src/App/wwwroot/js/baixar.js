window.baixarArquivo = (nomeArquivo, base64) => {
    const link = document.createElement('a');
    link.download = nomeArquivo;
    link.href = "data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64," + base64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

window.copiarTexto = async (texto) => {
    try {
        await navigator.clipboard.writeText(texto);
        return true;
    } catch (erro) {
        // Fallback para quando a área de transferência é bloqueada pelo navegador.
        const campo = document.createElement('textarea');
        campo.value = texto;
        campo.style.position = 'fixed';
        campo.style.opacity = '0';
        document.body.appendChild(campo);
        campo.select();
        const funcionou = document.execCommand('copy');
        document.body.removeChild(campo);
        return funcionou;
    }
};
