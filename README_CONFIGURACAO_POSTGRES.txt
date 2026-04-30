# ZTR Launcher - PostgreSQL Real + Online Real

Usei seus arquivos:
- MainForm.cs enviado
- ztr_launcher.json enviado

## Importante sobre seu ztr_launcher.json
O campo estava errado:
serverStatusUrl estava como:
https://servidor-ztr-company-launcher.onrender.com/heartbeat/game/status

O correto é:
https://servidor-ztr-company-launcher.onrender.com/status

Já deixei corrigido em:
ztr_launcher_CORRIGIDO.json

## Render PostgreSQL
Agora o login/registro é real usando PostgreSQL.

No Render:
1. Crie um PostgreSQL.
2. Copie a Internal Database URL ou External Database URL.
3. No seu Web Service, vá em Environment.
4. Adicione:
DATABASE_URL= SUA_URL_DO_POSTGRES

## Render Web Service
Build Command:
npm install

Start Command:
node server.js

## Endpoints reais
POST /auth/register
POST /auth/login
POST /profile/update
POST /heartbeat/launcher
POST /heartbeat/game
POST /offline
GET /status

## O que o launcher agora faz
- Registro real no PostgreSQL.
- Login real no PostgreSQL.
- Perfil salvo no PostgreSQL.
- Lista todas as contas em Servidores.
- Mostra players online no launcher.
- Mostra players jogando.
- Clicar no perfil mostra status e bio.
- Ping real medido pelo tempo do GET /status.
- Ao fechar launcher, envia offline.
- Ao fechar jogo, envia offline do jogo.
