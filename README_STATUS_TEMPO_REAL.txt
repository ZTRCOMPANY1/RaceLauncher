# ZTR Launcher - Servidores em tempo real

Agora a aba Servidores busca o endpoint:

GET /status

E mostra:
- Pessoas online no ZTR Launcher
- Pessoas jogando Race Low Poly
- Lista de jogadores no jogo

## Configurar no ztr_launcher.json

Coloque:

```json
"serverStatusUrl": "https://SEU-SERVIDOR.onrender.com/status",
"launcherHeartbeatUrl": "https://SEU-SERVIDOR.onrender.com/heartbeat/launcher",
"gameHeartbeatUrl": "https://SEU-SERVIDOR.onrender.com/heartbeat/game"
```

## Render

Build Command:
npm install

Start Command:
node server_heartbeat_exemplo.js

## Testar

Abra no navegador:

https://SEU-SERVIDOR.onrender.com/status

Deve aparecer:

```json
{
  "launcherOnline": 0,
  "gameOnline": 0,
  "launcherUsers": [],
  "gameUsers": []
}
```

## Observação

Se o GET /status falhar, a aba Servidores usa os dados antigos do ztr_launcher.json como fallback.
