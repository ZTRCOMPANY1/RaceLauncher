# ZTR Launcher - Conta obrigatória + Perfil Steam + Heartbeat

## O que foi adicionado

1. Criar conta obrigatória
- Sem conta/login, o botão Jogar fica bloqueado.
- Username + senha.
- A senha fica salva localmente como SHA256 com salt simples.

2. Perfil melhorado
- Avatar por URL.
- @username.
- Nome exibido.
- Bio editável.
- Horas jogadas reais.
- Conquistas.

3. Tempo real de jogo
- Ao clicar em Jogar, o launcher abre o jogo e monitora o processo.
- Enquanto o jogo estiver aberto, envia heartbeat do jogo a cada 15s.
- Quando fechar o jogo, soma o tempo real no perfil.

4. Heartbeat / rastreador online
No ztr_launcher.json, configure:

```json
"launcherHeartbeatUrl": "https://SEU-SERVIDOR.com/heartbeat/launcher",
"gameHeartbeatUrl": "https://SEU-SERVIDOR.com/heartbeat/game"
```

## Servidor de heartbeat

Incluí o arquivo: server_heartbeat_exemplo.js

Para rodar local:

```bash
npm init -y
npm install express cors
node server_heartbeat_exemplo.js
```

Endpoints:
- POST /heartbeat/launcher
- POST /heartbeat/game
- GET /status

No Render:
1. Crie Web Service Node.js.
2. Suba server_heartbeat_exemplo.js.
3. Start command: node server_heartbeat_exemplo.js
4. Coloque a URL no ztr_launcher.json.

## Observação importante

A aba Servidores ainda lê números do ztr_launcher.json.
O heartbeat já envia dados para o servidor. O próximo upgrade é fazer o launcher buscar GET /status e atualizar a aba Servidores automaticamente com números reais.
