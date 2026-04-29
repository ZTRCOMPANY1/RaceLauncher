# ZTR Company Launcher - Upgrade Completo

Este projeto foi gerado em cima do seu MainForm.cs enviado.

## O que foi adicionado

1. Login ZTR Company local
- Aba PERFIL
- Nome do jogador
- Avatar por URL
- Dados salvos em:
  %localappdata%\ZTRCompanyLauncher\config.json

2. Página de perfil
- Horas jogadas
- Última vez online
- Versão instalada por jogo
- Conquistas locais

Observação:
As horas jogadas são simuladas por enquanto.
Cada vez que clicar em Jogar, soma 0.1h.
Para contar tempo real, precisa monitorar o processo do jogo até ele fechar.

3. Sistema de eventos
No início aparece o painel Eventos.
Configure pelo ztr_launcher.json:

"events": [
  {
    "title": "Evento de corrida",
    "message": "Evento especial começa hoje!",
    "active": true
  }
]

4. Sistema de servidores
Nova aba SERVIDORES.
Configure no JSON:

"servers": [
  {
    "name": "Race Low Poly - Principal",
    "online": true,
    "pingMs": 38,
    "playersOnline": 12,
    "maxPlayers": 50
  }
]

5. Notificações
O launcher mostra popup quando:
- Um jogo novo aparece no JSON
- Uma versão nova aparece
- Eventos mudam
- Instalação termina
- Update do launcher aparece

6. Auto-update real do launcher
Já usa:
"launcherVersion"
"launcherUpdateUrl"
"launcherSha256"

Exemplo:

"launcherVersion": "1.0.1",
"launcherUpdateUrl": "https://seusite.com/ZTRCompanyLauncher.exe",
"launcherSha256": ""

Se colocar hash SHA256, ele valida antes de substituir.

## Como rodar

dotnet run

## Como publicar

dotnet publish -c Release -r win-x64 --self-contained true

## Arquivos importantes

- MainForm.cs
- Program.cs
- ZTRCompanyLauncher.csproj
- ztr_startup_chime.wav
- ztr_enter_confirm.wav
- ztr_launcher_EXEMPLO.json

## Subir no GitHub Pages

Suba o seu ztr_launcher.json com os novos campos:
- news
- events
- servers
- games
- launcherVersion
- launcherUpdateUrl
- launcherSha256

## Importante

Seu arquivo original está citado na conversa como base da alteração.
