# ZTR Launcher - FINAL com Login/Registro + Servidores reais

Arquivos principais:
- MainForm.cs atualizado
- server.js para Render
- package.json
- ztr_launcher_EXEMPLO.json

## O que foi corrigido/adicionado
- Registro obrigatório se não tiver conta.
- Login obrigatório se já tiver conta e estiver deslogado.
- Perfil com avatar, nome, bio, estatísticas e conquistas.
- Botão Jogar bloqueado sem login.
- Heartbeat do launcher para /heartbeat/launcher.
- Heartbeat do jogo para /heartbeat/game enquanto o jogo estiver aberto.
- Aba Servidores lê /status e atualiza sozinha a cada 5 segundos.
- Fallback direto para:
  https://servidor-ztr-company-launcher.onrender.com/status

## Render
Build Command:
npm install

Start Command:
node server.js

## Teste
Abra:
https://servidor-ztr-company-launcher.onrender.com/status

Abra o launcher: launcherOnline deve subir.
Clique em jogar: gameOnline deve subir.
