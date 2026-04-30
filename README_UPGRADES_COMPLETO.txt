# ZTR Launcher - Upgrades completos

Incluído neste pacote:

## Launcher / MainForm.cs
- Sistema de conquistas online.
- Sincronização de conquistas com o backend.
- Perfil mostra conquistas locais e online.
- Botão ADMIN na sidebar.
- Auto-update profissional:
  1. baixa o ZIP da nova versão;
  2. valida SHA256 se você colocar no JSON;
  3. extrai;
  4. cria updater .bat;
  5. fecha o launcher;
  6. substitui arquivos;
  7. reabre automaticamente.

## Backend / server.js
- PostgreSQL.
- Usuários.
- Banimento.
- Heartbeat launcher/game.
- Conquistas reais.
- Notificações admin.
- Estatísticas.

## Painel admin
Acesse:
https://SEU-SERVIDOR.onrender.com/admin

Configure no Render:
ADMIN_KEY=sua_senha_admin
DATABASE_URL=sua_url_postgresql

## Render
Build Command:
npm install

Start Command:
npm start

## Auto-update
No seu ztr_launcher.json coloque:

"launcherVersion": "1.0.1",
"launcherUpdateUrl": "https://link-do-seu-zip/launcher_update.zip",
"launcherSha256": "hash_sha256_opcional"

O ZIP do update deve conter os arquivos novos do launcher na raiz:
- ZTRCompanyLauncher.exe
- dlls
- arquivos necessários

Se deixar launcherSha256 vazio, ele atualiza sem validar hash.
