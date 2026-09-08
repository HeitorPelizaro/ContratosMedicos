# Controle de Contratos Médicos — como instalar e usar

## 1. Instalar (uma vez)

1. Baixe o arquivo `Instalador-ContratosMedicos-1.0.exe` do link que te enviei.
2. Dê dois cliques nele.
3. **Se aparecer a tela azul "O Windows protegeu seu computador"**, isso é normal:
   o programa é novo e não tem certificado pago. Clique em **Mais informações** e
   depois em **Executar assim mesmo**.
4. Clique em **Install** e depois em **Close**.
5. Vai aparecer o ícone **Controle de Contratos Médicos** na sua área de trabalho.

Não precisa de senha de administrador. Se o antivírus do trabalho bloquear, me chame:
a TI precisa liberar o programa.

## 2. Trazer os contratos da planilha (uma vez)

1. Abra o programa pelo ícone da área de trabalho.
2. No menu da esquerda, clique em **Importar planilha**.
3. Escolha o seu arquivo `2026_Relatorio de Contratos.xlsx`.
4. O programa mostra as colunas que reconheceu — confira e corrija se alguma estiver trocada.
5. Clique em **Conferir os dados**. Vai aparecer um relatório: quantos contratos entraram,
   quais datas não deu para entender, se tem contrato repetido. **Nada é jogado fora**:
   o que ele não entendeu aparece depois na tela **Atenção** para você arrumar.
6. Deixe marcado *"Adicionar novos e atualizar os que já existem"* e clique em **Importar agora**.

A sua planilha **não é alterada** — o programa só lê.

## 3. O dia a dia

- **Painel**: os números de sempre, atualizados na hora — total, assinados, pendentes de
  assinatura, vigência vencida, vencendo em até 30 dias, com problema ou dúvida.
  Clique em qualquer número para ver a lista daqueles contratos.
- **Contratos**: digite na busca o nome do médico, a empresa, o CNPJ ou o código Tasy.
  Clique numa linha para abrir, editar, dar baixa em assinatura, lançar aditivo ou anexar o PDF.
  O botão **Exportar Excel** gera uma planilha só com o que está aparecendo na tela.
- **Atenção**: a fila do que precisa de providência, do mais urgente para o menos.
- **Aditivos**: dentro de cada contrato. Aditivo de valor fora da época de renovação
  entra normalmente — escolha o tipo **Valor** e ele não mexe na vigência.
  Aditivo de prazo (tipo **Vigência**), quando marcado como assinado, passa a valer
  como a nova data de vencimento do contrato.

## 4. O relatório mensal da gerência

1. Clique em **Indicadores** no menu.
2. O mês que já vem escolhido é o mês passado — normalmente é esse que você envia.
   Se precisar de outro, troque no seletor de mês.
3. Você vê duas partes: a **posição no fim do mês** (quantos médicos, quantos contratos,
   quantos assinados e o percentual) e o **movimento do mês** (quantos foram assinados
   naquele mês). Embaixo, o detalhe por especialidade.
4. Clique em **Copiar para o e-mail** e cole direto no Outlook, ou em **Exportar Excel**
   se a gerência preferir a planilha anexada.

Detalhe importante: quando o mês vira, o programa "tira uma foto" dos números daquele mês.
A partir daí eles não mudam mais, mesmo que você cadastre contratos novos depois — assim o
número que você mandou em agosto continua sendo o mesmo se alguém for conferir em dezembro.
Enquanto o mês está aberto, a tela avisa que os números ainda vão mudar.

## 5. Backup (importante)

Seus dados ficam em uma pasta só sua, no seu computador. O caminho está escrito na tela
**Configurações**. O programa já guarda uma cópia por dia (as 7 últimas) e, além dessas,
uma cópia antes de cada importação (também as 7 últimas). Uma contagem não come a outra:
por mais importações que você faça num dia, os 7 dias de cópia diária continuam ali.
Mesmo assim, de vez em quando:

1. Abra **Configurações** e veja o caminho da pasta.
2. Copie essa pasta inteira para um pendrive ou para o OneDrive.

## 6. Se algo der errado

Me chame e me diga o que apareceu na tela. Se possível, mande também a pasta `log`
que está dentro da pasta de dados — é lá que o programa anota o que aconteceu.
