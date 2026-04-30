# ZTR Launcher - Discord Rich Presence

Arquivos modificados:
- MainForm.cs
- ZTRCompanyLauncher.csproj

## O que foi adicionado

Quando abrir o launcher:
Discord mostra atividade do ZTR Company Launcher.

Quando clicar em Jogar:
Discord muda para Race Low Poly / nome do jogo.

Quando fechar o jogo:
Discord volta para ZTR Company Launcher.

Quando fechar o launcher:
A presença é limpa.

## Configuração obrigatória

No MainForm.cs, procure:

private const string DISCORD_APPLICATION_ID = "COLOQUE_SEU_APPLICATION_ID_AQUI";

Troque pelo Application ID do seu app no Discord Developer Portal.

## Criar app no Discord

1. Acesse Discord Developer Portal.
2. Applications.
3. New Application.
4. Nome: ZTR Company Launcher.
5. Copie o Application ID.
6. Cole no MainForm.cs.

## Imagens no Discord

No Developer Portal, vá em Rich Presence > Art Assets e envie imagens com estes nomes:

ztr_logo
race_low_poly
online

Se não enviar imagens, a presença ainda pode funcionar, mas sem imagem.

## Dependência

O projeto usa o NuGet:

DiscordRichPresence

Se quiser instalar manualmente:

dotnet add package DiscordRichPresence

Depois rode:

dotnet restore
dotnet run
