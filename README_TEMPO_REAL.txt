# ZTR Company Launcher - Tempo Real de Jogo

Implementado:
- Ao clicar em Jogar, o launcher abre o jogo e monitora o processo.
- O tempo só é somado quando o jogo fecha.
- As horas reais são salvas no perfil.
- Ao fechar o jogo, aparece uma notificação com o tempo da sessão.
- Conquistas:
  - Primeiro jogo iniciado
  - Jogou por 1 minuto
  - 1 hora jogada

Como rodar:
dotnet run

Se der erro de arquivo travado no dotnet watch:
taskkill /F /IM dotnet.exe
taskkill /F /IM ZTRCompanyLauncher.exe

Depois apague bin e obj e rode:
dotnet run
