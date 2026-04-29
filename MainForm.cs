using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Media;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZTRCompanyLauncher
{
    public class MainForm : Form
    {
        private const string CURRENT_LAUNCHER_VERSION = "1.0.0";

        private readonly string launcherJsonUrl =
            "https://ztrcompany1.github.io/SITE-MENSAGEM-NO-JOGO-RACE-LOW-POLY/ztr_launcher.json";

        private readonly string appData =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZTRCompanyLauncher");

        private readonly string configFile;
        private readonly string tempFolder;

        private LauncherOnlineData? onlineData;
        private LauncherConfig config;
        private GameData? selectedGame;

        private Panel root = null!;
        private Panel topbar = null!;
        private Panel content = null!;
        private Panel splash = null!;
        private FlowLayoutPanel gameList = null!;
        private Panel detailsPanel = null!;

        private Label logoLabel = null!;
        private Label statusLabel = null!;
        private Label titleLabel = null!;
        private Label subtitleLabel = null!;
        private Label gameTitleLabel = null!;
        private Label versionLabel = null!;
        private Label pathLabel = null!;
        private Label downloadLabel = null!;
        private Label speedLabel = null!;
        private Label etaLabel = null!;

        private PictureBox bannerBox = null!;
        private PictureBox iconBox = null!;

        private TextBox newsBox = null!;
        private TextBox changelogBox = null!;

        private ZButton homeButton = null!;
        private ZButton gamesButton = null!;
        private ZButton profileButton = null!;
        private ZButton serversButton = null!;
        private ZButton fullscreenButton = null!;
        private ZButton launcherUpdateButton = null!;
        private ZButton installButton = null!;
        private ZButton updateButton = null!;
        private ZButton repairButton = null!;
        private ZButton playButton = null!;
        private ZButton folderButton = null!;
        private ZButton locationButton = null!;

        private ProgressBar progressBar = null!;

        private Timer autoCheckTimer = null!;
        private Timer blinkTimer = null!;
        private Timer splashTimer = null!;
        private Timer bootAnimTimer = null!;
        private Timer serverStatusTimer = null!;
        private ServerStatusData? liveServerStatus;
        private SoundPlayer? startupSound;
        private SoundPlayer? confirmSound;

        private bool busy = false;
        private bool isFullScreen = false;
        private bool blink = false;
        private int bootStep = 0;
        private string lastNotificationKey = "";

        private Rectangle oldBounds;
        private FormBorderStyle oldBorder;

        public MainForm()
        {
            configFile = Path.Combine(appData, "config.json");
            tempFolder = Path.Combine(appData, "_temp");

            Directory.CreateDirectory(appData);
            config = LoadConfig();

            Text = "ZTR Company Launcher";
            MinimumSize = new Size(1100, 700);
            Width = 1260;
            Height = 886;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(10, 14, 22);
            KeyPreview = true;
            DoubleBuffered = true;

            BuildInterface();
            BuildSplash();

            autoCheckTimer = new Timer { Interval = 5000 };
            autoCheckTimer.Tick += async (s, e) => await LoadOnlineData(true);

            blinkTimer = new Timer { Interval = 500 };
            blinkTimer.Tick += (s, e) => BlinkUpdateButton();

            splashTimer = new Timer { Interval = 2600 };
            splashTimer.Tick += (s, e) =>
            {
                splashTimer.Stop();
                splash.Visible = false;
                PlayEnterSound();
            };

            bootAnimTimer = new Timer { Interval = 55 };
            bootAnimTimer.Tick += (s, e) => AnimateBoot();

            serverStatusTimer = new Timer { Interval = 5000 };
            serverStatusTimer.Tick += async (s, e) => await LoadLiveServerStatus();
                await SendHeartbeatAsync("launcher", null);

            Resize += (s, e) => LayoutResponsive();
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F11)
                    ToggleFullscreen();
            };

            Load += async (s, e) =>
            {
                PlayStartupSound();

                splash.Visible = true;
                splash.BringToFront();
                bootAnimTimer.Start();
                splashTimer.Start();

                ShowHome();
                await LoadOnlineData(false);
                autoCheckTimer.Start();
                serverStatusTimer.Start();
                await LoadLiveServerStatus();
            };
        }

        private void BuildInterface()
        {
            root = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 14, 22)
            };

            topbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Color.FromArgb(16, 22, 34)
            };

            content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 14, 22)
            };

            logoLabel = new Label
            {
                Text = "ZTR COMPANY",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 21, FontStyle.Bold),
                Left = 26,
                Top = 18,
                Width = 230,
                Height = 42
            };

            homeButton = MakeTopButton("INÍCIO", 270);
            gamesButton = MakeTopButton("JOGOS", 385);
            profileButton = MakeTopButton("PERFIL", 500);
            serversButton = MakeTopButton("SERVIDORES", 615);
            serversButton.Width = 130;

            fullscreenButton = MakeTopButton("TELA CHEIA", 760);
            fullscreenButton.Width = 135;
            fullscreenButton.Click += (s, e) => ToggleFullscreen();

            launcherUpdateButton = MakeTopButton("UPDATE LAUNCHER", 910);
            launcherUpdateButton.Width = 185;
            launcherUpdateButton.Visible = false;
            launcherUpdateButton.Click += async (s, e) => await UpdateLauncher();

            statusLabel = new Label
            {
                Text = "Inicializando...",
                ForeColor = Color.FromArgb(170, 185, 205),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Top = 24,
                Width = 330,
                Height = 28
            };

            homeButton.Click += (s, e) => ShowHome();
            gamesButton.Click += (s, e) => ShowGames();
            profileButton.Click += (s, e) => ShowProfile();
            serversButton.Click += (s, e) => ShowServers();

            topbar.Controls.Add(logoLabel);
            topbar.Controls.Add(homeButton);
            topbar.Controls.Add(gamesButton);
            topbar.Controls.Add(profileButton);
            topbar.Controls.Add(serversButton);
            topbar.Controls.Add(fullscreenButton);
            topbar.Controls.Add(launcherUpdateButton);
            topbar.Controls.Add(statusLabel);

            root.Controls.Add(content);
            root.Controls.Add(topbar);
            Controls.Add(root);

            LayoutResponsive();
        }

        private void BuildSplash()
        {
            splash = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(4, 8, 14),
                Visible = false
            };

            Label ztr = new Label
            {
                Name = "BootTitle",
                Text = "ZTR",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 54, FontStyle.Bold),
                Width = 420,
                Height = 90,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label company = new Label
            {
                Name = "BootSubtitle",
                Text = "COMPANY LAUNCHER",
                ForeColor = Color.FromArgb(50, 145, 255),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Width = 420,
                Height = 36,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label loading = new Label
            {
                Name = "BootLoading",
                Text = "●  ●  ●",
                ForeColor = Color.FromArgb(110, 180, 255),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Width = 420,
                Height = 36,
                TextAlign = ContentAlignment.MiddleCenter
            };

            splash.Controls.Add(ztr);
            splash.Controls.Add(company);
            splash.Controls.Add(loading);
            Controls.Add(splash);

            PositionSplash();
        }

        private void PositionSplash()
        {
            if (splash == null) return;

            foreach (Control c in splash.Controls)
                c.Left = (ClientSize.Width - c.Width) / 2;

            Control title = splash.Controls["BootTitle"];
            Control subtitle = splash.Controls["BootSubtitle"];
            Control loading = splash.Controls["BootLoading"];

            title.Top = (ClientSize.Height / 2) - 105;
            subtitle.Top = title.Bottom + 4;
            loading.Top = subtitle.Bottom + 24;
        }

        private void AnimateBoot()
        {
            bootStep++;

            Label? loading = splash.Controls["BootLoading"] as Label;
            Label? title = splash.Controls["BootTitle"] as Label;

            if (loading != null)
            {
                int phase = bootStep % 4;
                loading.Text = phase switch
                {
                    0 => "●  ○  ○",
                    1 => "○  ●  ○",
                    2 => "○  ○  ●",
                    _ => "●  ●  ●"
                };
            }

            if (title != null)
            {
                int glow = 180 + (int)(60 * Math.Sin(bootStep * 0.22));
                title.ForeColor = Color.FromArgb(Math.Min(255, glow), Math.Min(255, glow), 255);
            }
        }

        private void LayoutResponsive()
        {
            statusLabel.Left = Math.Max(860, ClientSize.Width - 360);
            launcherUpdateButton.Left = Math.Min(910, Math.Max(760, ClientSize.Width - 560));
            PositionSplash();

            if (newsBox != null && newsBox.Parent != null)
            {
                newsBox.Width = content.Width - 80;
                newsBox.Height = content.Height - 175;
            }

            if (gameList != null && gameList.Parent != null)
            {
                int leftWidth = Math.Max(320, (int)(content.Width * 0.28));
                gameList.Parent.Width = leftWidth;
                gameList.Parent.Height = content.Height - 115;
                gameList.Width = leftWidth - 28;
                gameList.Height = gameList.Parent.Height - 28;

                detailsPanel.Left = leftWidth + 58;
                detailsPanel.Width = content.Width - detailsPanel.Left - 38;
                detailsPanel.Height = content.Height - 115;

                ResizeGameDetails();
            }
        }

        private void ResizeGameDetails()
        {
            if (detailsPanel == null || bannerBox == null) return;

            int margin = 30;
            int panelWidth = detailsPanel.Width;
            int panelHeight = detailsPanel.Height;
            int contentWidth = Math.Max(300, panelWidth - (margin * 2));

            bannerBox.Left = 22;
            bannerBox.Top = 18;
            bannerBox.Width = Math.Max(320, panelWidth - 44);
            bannerBox.Height = Math.Max(150, Math.Min(235, (int)(bannerBox.Width / 1.777)));

            iconBox.Left = margin;
            iconBox.Top = bannerBox.Bottom + 18;
            iconBox.Width = 62;
            iconBox.Height = 62;

            gameTitleLabel.Left = iconBox.Right + 16;
            gameTitleLabel.Top = iconBox.Top;
            gameTitleLabel.Width = Math.Max(250, panelWidth - gameTitleLabel.Left - margin);
            gameTitleLabel.Height = 32;

            versionLabel.Left = gameTitleLabel.Left;
            versionLabel.Top = gameTitleLabel.Bottom + 2;
            versionLabel.Width = gameTitleLabel.Width;

            pathLabel.Left = margin;
            pathLabel.Top = iconBox.Bottom + 12;
            pathLabel.Width = contentWidth;
            pathLabel.Height = 40;

            int buttonTop = Math.Max(525, panelHeight - 58);

            etaLabel.Left = margin + Math.Max(260, contentWidth / 2);
            etaLabel.Width = Math.Max(240, contentWidth / 2 - 10);
            speedLabel.Left = margin;
            speedLabel.Width = Math.Max(240, contentWidth / 2 - 10);

            speedLabel.Top = buttonTop - 54;
            etaLabel.Top = speedLabel.Top;
            downloadLabel.Left = margin;
            downloadLabel.Top = speedLabel.Top - 25;
            downloadLabel.Width = contentWidth;

            progressBar.Left = margin;
            progressBar.Top = downloadLabel.Top - 34;
            progressBar.Width = contentWidth;
            progressBar.Height = 24;

            changelogBox.Left = margin;
            changelogBox.Top = pathLabel.Bottom + 14;
            changelogBox.Width = contentWidth;
            changelogBox.Height = Math.Max(70, progressBar.Top - changelogBox.Top - 18);

            ZButton[] btns = { installButton, updateButton, repairButton, playButton, folderButton, locationButton };
            int gap = 10;
            int btnW = Math.Max(88, (contentWidth - (gap * (btns.Length - 1))) / btns.Length);

            for (int i = 0; i < btns.Length; i++)
            {
                btns[i].Top = buttonTop;
                btns[i].Left = margin + i * (btnW + gap);
                btns[i].Width = btnW;
                btns[i].Height = 42;
            }
        }

        private ZButton MakeTopButton(string text, int left)
        {
            ZButton b = new ZButton
            {
                Text = text,
                Left = left,
                Top = 18,
                Width = 96,
                Height = 42,
                NormalColor = Color.FromArgb(31, 41, 58),
                HoverColor = Color.FromArgb(46, 73, 112),
                PressColor = Color.FromArgb(65, 135, 255),
                DisabledColor = Color.FromArgb(45, 50, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            b.ApplyVisualState();
            return b;
        }

        private ZButton MakeButton(string text, int left, int top, int width, int height)
        {
            ZButton b = new ZButton
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                NormalColor = Color.FromArgb(42, 116, 255),
                HoverColor = Color.FromArgb(70, 145, 255),
                PressColor = Color.FromArgb(28, 86, 210),
                DisabledColor = Color.FromArgb(45, 50, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            b.ApplyVisualState();
            return b;
        }

        private Label MakeLabel(string text, int left, int top, int width, int height, int size = 10, bool bold = false)
        {
            return new Label
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                ForeColor = Color.FromArgb(218, 228, 242),
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular)
            };
        }

        private TextBox MakeBox(int left, int top, int width, int height)
        {
            return new TextBox
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(18, 25, 36),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };
        }

        private Panel MakeCard(int left, int top, int width, int height)
        {
            return new Panel
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                BackColor = Color.FromArgb(14, 20, 31)
            };
        }

        private void ShowHome()
        {
            content.Controls.Clear();

            titleLabel = MakeLabel("Início", 40, 30, 800, 48, 27, true);
            subtitleLabel = MakeLabel("Notícias, eventos e status da ZTR Company", 42, 80, 800, 28, 11);

            Panel eventsPanel = MakeCard(40, 120, content.Width - 80, 130);
            Label eventsTitle = MakeLabel("Eventos", 18, 12, 500, 28, 15, true);
            Label eventsBody = MakeLabel(GetEventsText(), 18, 45, eventsPanel.Width - 36, 75, 10);
            eventsPanel.Controls.Add(eventsTitle);
            eventsPanel.Controls.Add(eventsBody);

            newsBox = MakeBox(40, 270, content.Width - 80, Math.Max(220, content.Height - 320));

            content.Controls.Add(titleLabel);
            content.Controls.Add(subtitleLabel);
            content.Controls.Add(eventsPanel);
            content.Controls.Add(newsBox);

            FillNews();
            LayoutResponsive();
        }

        private string GetEventsText()
        {
            if (onlineData?.events == null || onlineData.events.Count == 0)
                return "Nenhum evento ativo no momento.";

            return string.Join(Environment.NewLine, onlineData.events.Select(e =>
            {
                string status = e.active ? "ATIVO" : "AGENDADO";
                return $"[{status}] {e.title} - {e.message}";
            }));
        }

        private void ShowProfile()
        {
            content.Controls.Clear();

            titleLabel = MakeLabel("Perfil ZTR Company", 40, 30, 800, 48, 27, true);

            Panel loginPanel = MakeCard(40, 100, 420, 420);

            Label nameLabel = MakeLabel("Nome do jogador:", 25, 25, 300, 25, 10, true);
            TextBox nameInput = MakeBox(25, 55, 360, 35);
            nameInput.Multiline = false;
            nameInput.Text = config.playerName;

            Label avatarLabel = MakeLabel("URL do avatar:", 25, 105, 300, 25, 10, true);
            TextBox avatarInput = MakeBox(25, 135, 360, 35);
            avatarInput.Multiline = false;
            avatarInput.Text = config.avatarUrl;

            PictureBox avatarBox = new PictureBox
            {
                Left = 25,
                Top = 190,
                Width = 96,
                Height = 96,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(22, 31, 45)
            };

            Label accountInfo = MakeLabel("", 140, 190, 240, 110, 10);

            ZButton saveButton = MakeButton("Salvar Login", 25, 325, 160, 42);
            ZButton logoutButton = MakeButton("Sair da conta", 205, 325, 160, 42);

            saveButton.Click += async (s, e) =>
            {
                config.playerName = string.IsNullOrWhiteSpace(nameInput.Text) ? "Player ZTR" : nameInput.Text.Trim();
                config.avatarUrl = avatarInput.Text.Trim();
                config.lastOnline = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                SaveConfig(config);
                ShowNotification("Login salvo", "Conta local salva com sucesso.", "login");
                ShowProfile();
                await Task.CompletedTask;
            };

            logoutButton.Click += (s, e) =>
            {
                config.playerName = "Player ZTR";
                config.avatarUrl = "";
                SaveConfig(config);
                ShowNotification("Logout", "Você saiu da conta local.", "logout");
                ShowProfile();
            };

            loginPanel.Controls.Add(nameLabel);
            loginPanel.Controls.Add(nameInput);
            loginPanel.Controls.Add(avatarLabel);
            loginPanel.Controls.Add(avatarInput);
            loginPanel.Controls.Add(avatarBox);
            loginPanel.Controls.Add(accountInfo);
            loginPanel.Controls.Add(saveButton);
            loginPanel.Controls.Add(logoutButton);

            Panel statsPanel = MakeCard(500, 100, content.Width - 540, 420);
            Label statsTitle = MakeLabel("Estatísticas", 25, 25, 600, 30, 16, true);
            TextBox statsBox = MakeBox(25, 70, statsPanel.Width - 50, 310);
            statsBox.Text = BuildProfileStatsText();

            statsPanel.Controls.Add(statsTitle);
            statsPanel.Controls.Add(statsBox);

            content.Controls.Add(titleLabel);
            content.Controls.Add(loginPanel);
            content.Controls.Add(statsPanel);

            accountInfo.Text =
                $"Jogador: {config.playerName}{Environment.NewLine}" +
                $"Última vez online: {config.lastOnline}{Environment.NewLine}" +
                $"Conquistas: {config.achievements.Count}";

            if (!string.IsNullOrWhiteSpace(config.avatarUrl))
                _ = LoadImageAsync(config.avatarUrl, avatarBox);
        }

        private string BuildProfileStatsText()
        {
            List<string> lines = new List<string>();

            lines.Add("=== PERFIL ===");
            lines.Add("Nome: " + config.playerName);
            lines.Add("Última vez online: " + config.lastOnline);
            lines.Add("");

            lines.Add("=== JOGOS ===");

            if (onlineData?.games != null)
            {
                foreach (GameData game in onlineData.games)
                {
                    string localVersion = ReadLocalGameVersion(game);
                    string hours = config.playedHours.TryGetValue(game.id, out double h) ? h.ToString("0.0") : "0.0";

                    lines.Add($"{game.name}");
                    lines.Add($"  Versão instalada: {localVersion}");
                    lines.Add($"  Horas jogadas: {hours}h");
                    lines.Add("");
                }
            }

            lines.Add("=== CONQUISTAS ===");
            if (config.achievements.Count == 0)
            {
                lines.Add("Nenhuma conquista ainda.");
            }
            else
            {
                foreach (string a in config.achievements)
                    lines.Add("- " + a);
            }

            return string.Join(Environment.NewLine, lines);
        }

        private async void ShowServers()
        {
            content.Controls.Clear();

            await LoadLiveServerStatus();

            titleLabel = MakeLabel("Servidores", 40, 30, 800, 48, 27, true);
            subtitleLabel = MakeLabel("Status real do ZTR Launcher e dos jogos", 42, 80, 800, 28, 11);

            FlowLayoutPanel serversFlow = new FlowLayoutPanel
            {
                Left = 40,
                Top = 125,
                Width = content.Width - 80,
                Height = content.Height - 160,
                AutoScroll = true,
                BackColor = Color.FromArgb(10, 14, 22)
            };

            if (liveServerStatus != null)
            {
                Panel launcherCard = BuildLiveServerCard(
                    "ZTR Launcher",
                    true,
                    0,
                    liveServerStatus.launcherOnline,
                    0,
                    "Pessoas online no launcher agora"
                );

                Panel gameCard = BuildLiveServerCard(
                    "Race Low Poly",
                    true,
                    0,
                    liveServerStatus.gameOnline,
                    0,
                    "Pessoas jogando agora"
                );

                serversFlow.Controls.Add(launcherCard);
                serversFlow.Controls.Add(gameCard);

                if (liveServerStatus.gameUsers != null && liveServerStatus.gameUsers.Count > 0)
                {
                    Panel usersCard = new Panel
                    {
                        Width = Math.Max(700, serversFlow.Width - 35),
                        Height = 150,
                        BackColor = Color.FromArgb(14, 20, 31),
                        Margin = new Padding(0, 0, 0, 14)
                    };

                    Label usersTitle = MakeLabel("Jogadores no jogo", 20, 15, 600, 30, 15, true);
                    TextBox usersBox = MakeBox(20, 55, usersCard.Width - 40, 75);
                    usersBox.Text = string.Join(Environment.NewLine, liveServerStatus.gameUsers.Select(u =>
                        $"{u.playerName} (@{u.username}) - {u.gameId}"
                    ));

                    usersCard.Controls.Add(usersTitle);
                    usersCard.Controls.Add(usersBox);
                    serversFlow.Controls.Add(usersCard);
                }
            }
            else if (onlineData?.servers != null && onlineData.servers.Count > 0)
            {
                foreach (ServerData server in onlineData.servers)
                {
                    serversFlow.Controls.Add(BuildLiveServerCard(
                        server.name,
                        server.online,
                        server.pingMs,
                        server.playersOnline,
                        server.maxPlayers,
                        "Dados vindos do ztr_launcher.json"
                    ));
                }
            }
            else
            {
                Label empty = MakeLabel(
                    "Nenhum dado real encontrado. Configure serverStatusUrl no ztr_launcher.json.",
                    10,
                    10,
                    900,
                    30,
                    12
                );
                serversFlow.Controls.Add(empty);
            }

            content.Controls.Add(titleLabel);
            content.Controls.Add(subtitleLabel);
            content.Controls.Add(serversFlow);
        }

        private Panel BuildLiveServerCard(string name, bool online, int pingMs, int playersOnline, int maxPlayers, string description)
        {
            Panel card = new Panel
            {
                Width = Math.Max(700, content.Width - 115),
                Height = 115,
                BackColor = Color.FromArgb(14, 20, 31),
                Margin = new Padding(0, 0, 0, 14)
            };

            Color dot = online ? Color.LimeGreen : Color.IndianRed;

            string playersText = maxPlayers > 0
                ? $"{playersOnline}/{maxPlayers}"
                : playersOnline.ToString();

            string pingText = pingMs > 0 ? $"{pingMs}ms" : "tempo real";

            Label title = MakeLabel(name, 20, 15, 450, 30, 15, true);
            Label info = MakeLabel(
                $"Status: {(online ? "Online" : "Offline")}   |   Ping: {pingText}   |   Online: {playersText}",
                20,
                52,
                720,
                26,
                11
            );

            Label desc = MakeLabel(description, 20, 78, 720, 24, 9);

            Panel statusDot = new Panel
            {
                Left = card.Width - 55,
                Top = 38,
                Width = 22,
                Height = 22,
                BackColor = dot
            };

            card.Controls.Add(title);
            card.Controls.Add(info);
            card.Controls.Add(desc);
            card.Controls.Add(statusDot);

            return card;
        }

        private async Task LoadLiveServerStatus()
        {
            try
            {
                if (onlineData == null || string.IsNullOrWhiteSpace(onlineData.serverStatusUrl))
                    return;

                using HttpClient client = new HttpClient();
                string json = await client.GetStringAsync(onlineData.serverStatusUrl + "?v=" + DateTime.Now.Ticks);

                ServerStatusData? status = JsonSerializer.Deserialize<ServerStatusData>(json);

                if (status != null)
                    liveServerStatus = status;
            }
            catch
            {
                liveServerStatus = null;
            }
        }

        private void ShowGames()
        {
            content.Controls.Clear();

            titleLabel = MakeLabel("Biblioteca", 40, 28, 780, 48, 27, true);

            Panel leftPanel = new Panel
            {
                Left = 40,
                Top = 92,
                Width = 350,
                Height = content.Height - 115,
                BackColor = Color.FromArgb(14, 20, 31)
            };

            gameList = new FlowLayoutPanel
            {
                Left = 14,
                Top = 14,
                Width = leftPanel.Width - 28,
                Height = leftPanel.Height - 28,
                AutoScroll = true,
                BackColor = Color.FromArgb(14, 20, 31)
            };

            leftPanel.Controls.Add(gameList);

            detailsPanel = new Panel
            {
                Left = 430,
                Top = 92,
                Width = content.Width - 468,
                Height = content.Height - 115,
                BackColor = Color.FromArgb(14, 20, 31)
            };

            bannerBox = new PictureBox
            {
                Left = 22,
                Top = 18,
                Width = detailsPanel.Width - 44,
                Height = 230,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(22, 31, 45)
            };

            iconBox = new PictureBox
            {
                Left = 30,
                Top = 268,
                Width = 62,
                Height = 62,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(22, 31, 45)
            };

            gameTitleLabel = MakeLabel("Selecione um jogo", 108, 268, 700, 36, 21, true);
            versionLabel = MakeLabel("Versão: -", 108, 306, 700, 25, 10);
            pathLabel = MakeLabel("Local: -", 30, 345, 800, 42, 9);

            changelogBox = MakeBox(30, 405, detailsPanel.Width - 60, 115);

            progressBar = new ProgressBar
            {
                Left = 30,
                Top = 540,
                Width = detailsPanel.Width - 60,
                Height = 24
            };

            downloadLabel = MakeLabel("Download: -", 30, 570, 700, 22);
            speedLabel = MakeLabel("Velocidade: -", 30, 594, 280, 22);
            etaLabel = MakeLabel("Tempo restante: -", 360, 594, 320, 22);

            installButton = MakeButton("Instalar", 30, 635, 100, 42);
            updateButton = MakeButton("Atualizar", 140, 635, 100, 42);
            repairButton = MakeButton("Reparar", 250, 635, 100, 42);
            playButton = MakeButton("Jogar", 360, 635, 100, 42);
            folderButton = MakeButton("Pasta", 470, 635, 100, 42);
            locationButton = MakeButton("Local", 580, 635, 100, 42);

            installButton.Click += async (s, e) => await InstallOrUpdateSelectedGame(true, false);
            updateButton.Click += async (s, e) => await InstallOrUpdateSelectedGame(false, false);
            repairButton.Click += async (s, e) => await InstallOrUpdateSelectedGame(false, true);
            playButton.Click += (s, e) => PlaySelectedGame();
            folderButton.Click += (s, e) => OpenSelectedGameFolder();
            locationButton.Click += (s, e) => ChangeSelectedGameFolder();

            detailsPanel.Controls.Add(bannerBox);
            detailsPanel.Controls.Add(iconBox);
            detailsPanel.Controls.Add(gameTitleLabel);
            detailsPanel.Controls.Add(versionLabel);
            detailsPanel.Controls.Add(pathLabel);
            detailsPanel.Controls.Add(changelogBox);
            detailsPanel.Controls.Add(progressBar);
            detailsPanel.Controls.Add(downloadLabel);
            detailsPanel.Controls.Add(speedLabel);
            detailsPanel.Controls.Add(etaLabel);
            detailsPanel.Controls.Add(installButton);
            detailsPanel.Controls.Add(updateButton);
            detailsPanel.Controls.Add(repairButton);
            detailsPanel.Controls.Add(playButton);
            detailsPanel.Controls.Add(folderButton);
            detailsPanel.Controls.Add(locationButton);

            content.Controls.Add(titleLabel);
            content.Controls.Add(leftPanel);
            content.Controls.Add(detailsPanel);

            FillGameCards();
            LayoutResponsive();
        }

        private async Task LoadOnlineData(bool auto)
        {
            if (busy) return;

            try
            {
                using HttpClient client = new HttpClient();
                string json = await client.GetStringAsync(launcherJsonUrl + "?v=" + DateTime.Now.Ticks);

                LauncherOnlineData? newOnlineData = JsonSerializer.Deserialize<LauncherOnlineData>(json);

                if (newOnlineData == null)
                {
                    statusLabel.Text = "JSON inválido";
                    return;
                }

                DetectNotifications(onlineData, newOnlineData);

                onlineData = newOnlineData;
                config.lastOnline = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                SaveConfig(config);

                statusLabel.Text = "ONLINE";

                launcherUpdateButton.Visible =
                    !string.IsNullOrWhiteSpace(onlineData.launcherVersion) &&
                    onlineData.launcherVersion != CURRENT_LAUNCHER_VERSION &&
                    !string.IsNullOrWhiteSpace(onlineData.launcherUpdateUrl);

                if (launcherUpdateButton.Visible && !auto)
                    ShowNotification("Atualização do launcher", "Uma nova versão do launcher está disponível.", "launcher-update");

                FillNews();

                if (gameList != null)
                    FillGameCards();

                if (selectedGame != null)
                    RefreshGamePage();
            }
            catch (Exception ex)
            {
                if (!auto)
                    statusLabel.Text = "ERRO: " + ex.Message;
            }
        }

        private void DetectNotifications(LauncherOnlineData? oldData, LauncherOnlineData newData)
        {
            if (oldData == null)
                return;

            foreach (GameData newGame in newData.games)
            {
                GameData? oldGame = oldData.games.FirstOrDefault(g => g.id == newGame.id);

                if (oldGame == null)
                {
                    ShowNotification("Novo jogo adicionado", newGame.name + " entrou na biblioteca ZTR.", "new-game-" + newGame.id);
                    continue;
                }

                if (oldGame.version != newGame.version)
                {
                    ShowNotification("Nova atualização disponível", $"{newGame.name} recebeu a versão {newGame.version}.", "update-" + newGame.id + "-" + newGame.version);
                }
            }

            if (oldData.events.Count != newData.events.Count)
                ShowNotification("Novo evento", "A lista de eventos foi atualizada.", "events-" + newData.events.Count);
        }

        private void ShowNotification(string title, string message, string key)
        {
            if (lastNotificationKey == key)
                return;

            lastNotificationKey = key;

            NotifyForm popup = new NotifyForm(title, message);
            popup.StartPosition = FormStartPosition.Manual;
            Rectangle area = Screen.FromControl(this).WorkingArea;
            popup.Left = area.Right - popup.Width - 18;
            popup.Top = area.Bottom - popup.Height - 18;
            popup.Show();
        }

        private void FillNews()
        {
            if (newsBox == null) return;

            newsBox.Clear();

            if (onlineData?.news == null || onlineData.news.Count == 0)
            {
                newsBox.Text = "Nenhuma notícia disponível.";
                return;
            }

            foreach (NewsData n in onlineData.news)
            {
                newsBox.AppendText($"[{n.type.ToUpper()}] {n.title}{Environment.NewLine}");
                newsBox.AppendText(n.message + Environment.NewLine);

                if (!string.IsNullOrWhiteSpace(n.url))
                    newsBox.AppendText("Link: " + n.url + Environment.NewLine);

                newsBox.AppendText(Environment.NewLine);
            }
        }

        private void FillGameCards()
        {
            if (gameList == null || onlineData == null) return;

            gameList.Controls.Clear();

            foreach (GameData game in onlineData.games)
            {
                Panel card = new Panel
                {
                    Width = gameList.Width - 28,
                    Height = 88,
                    BackColor = selectedGame?.id == game.id ? Color.FromArgb(45, 93, 170) : Color.FromArgb(22, 31, 45),
                    Margin = new Padding(4, 4, 4, 12),
                    Cursor = Cursors.Hand
                };

                PictureBox icon = new PictureBox
                {
                    Left = 12,
                    Top = 12,
                    Width = 64,
                    Height = 64,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.FromArgb(30, 40, 56)
                };

                Label name = new Label
                {
                    Text = game.name,
                    Left = 90,
                    Top = 17,
                    Width = card.Width - 100,
                    Height = 25,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };

                Label version = new Label
                {
                    Text = "Versão " + game.version,
                    Left = 90,
                    Top = 45,
                    Width = card.Width - 100,
                    Height = 22,
                    ForeColor = Color.FromArgb(180, 195, 215),
                    Font = new Font("Segoe UI", 9)
                };

                void select()
                {
                    selectedGame = game;
                    FillGameCards();
                    RefreshGamePage();
                }

                card.Click += (s, e) => select();
                icon.Click += (s, e) => select();
                name.Click += (s, e) => select();
                version.Click += (s, e) => select();

                card.Controls.Add(icon);
                card.Controls.Add(name);
                card.Controls.Add(version);
                gameList.Controls.Add(card);

                _ = LoadImageAsync(game.iconUrl, icon);
            }

            if (selectedGame == null && onlineData.games.Count > 0)
            {
                selectedGame = onlineData.games[0];
                RefreshGamePage();
            }
        }

        private async Task LoadImageAsync(string url, PictureBox box)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url)) return;

                using HttpClient client = new HttpClient();
                byte[] data = await client.GetByteArrayAsync(url + "?v=" + DateTime.Now.Ticks);

                using MemoryStream ms = new MemoryStream(data);
                Image img = Image.FromStream(ms);

                box.Image = img;
            }
            catch { }
        }

        private void RefreshGamePage()
        {
            if (selectedGame == null) return;

            string installPath = GetInstallPath(selectedGame);
            string localVersion = ReadLocalGameVersion(selectedGame);
            bool installed = IsGameInstalled(selectedGame);
            bool needsUpdate = installed && localVersion != selectedGame.version;

            gameTitleLabel.Text = selectedGame.name;
            versionLabel.Text = installed
                ? $"Sua versão: {localVersion}  |  Online: {selectedGame.version}"
                : $"Sua versão: Não instalado  |  Online: {selectedGame.version}";

            pathLabel.Text = "Local: " + installPath;
            changelogBox.Text = selectedGame.changelog;

            _ = LoadImageAsync(selectedGame.iconUrl, iconBox);
            _ = LoadImageAsync(selectedGame.bannerUrl, bannerBox);

            installButton.Enabled = !installed && !busy;
            repairButton.Enabled = installed && !busy;
            folderButton.Enabled = Directory.Exists(installPath) && !busy;
            locationButton.Enabled = !busy;

            if (selectedGame.maintenance)
            {
                playButton.Enabled = false;
                updateButton.Enabled = false;
                statusLabel.Text = "MANUTENÇÃO";
                StopBlink();
                return;
            }

            if (!installed)
            {
                playButton.Enabled = false;
                updateButton.Enabled = false;
                StopBlink();
                return;
            }

            if (needsUpdate)
            {
                updateButton.Enabled = !busy;
                playButton.Enabled = !selectedGame.forceUpdate && !busy;
                statusLabel.Text = selectedGame.forceUpdate ? "UPDATE OBRIGATÓRIO" : "UPDATE DISPONÍVEL";
                StartBlink();
                return;
            }

            updateButton.Enabled = false;
            playButton.Enabled = !busy;
            statusLabel.Text = "JOGO ATUALIZADO";
            StopBlink();
        }

        private async Task InstallOrUpdateSelectedGame(bool firstInstall, bool repair)
        {
            if (selectedGame == null) return;

            DialogResult confirm = MessageBox.Show(
                "Instalar/atualizar em:\n\n" + GetInstallPath(selectedGame),
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm == DialogResult.No) return;

            try
            {
                busy = true;
                StopBlink();
                RefreshGamePage();

                progressBar.Value = 0;
                downloadLabel.Text = "Download: preparando...";
                speedLabel.Text = "Velocidade: -";
                etaLabel.Text = "Tempo restante: -";

                CloseGameProcess(selectedGame);

                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);

                Directory.CreateDirectory(tempFolder);

                string zipPath = Path.Combine(tempFolder, selectedGame.id + ".zip");
                string extractFolder = Path.Combine(tempFolder, "extracted");

                await DownloadFileWithProgress(selectedGame.downloadUrl, zipPath);

                if (!string.IsNullOrWhiteSpace(selectedGame.sha256))
                {
                    statusLabel.Text = "VERIFICANDO HASH";
                    string hash = ComputeSha256(zipPath);

                    if (!hash.Equals(selectedGame.sha256, StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Arquivo corrompido. SHA256 diferente.");
                }

                statusLabel.Text = "EXTRAINDO";
                ZipFile.ExtractToDirectory(zipPath, extractFolder);

                string exeFound = Directory.GetFiles(extractFolder, selectedGame.exeName, SearchOption.AllDirectories).FirstOrDefault() ?? "";

                if (string.IsNullOrWhiteSpace(exeFound))
                    throw new Exception("EXE não encontrado no ZIP: " + selectedGame.exeName);

                string realFolder = Path.GetDirectoryName(exeFound) ?? "";
                string installPath = GetInstallPath(selectedGame);

                statusLabel.Text = repair ? "REPARANDO" : firstInstall ? "INSTALANDO" : "ATUALIZANDO";

                Directory.CreateDirectory(installPath);
                ClearFolder(installPath);
                CopyFolder(realFolder, installPath);

                File.WriteAllText(Path.Combine(installPath, selectedGame.versionFileName), selectedGame.version);

                if (!config.achievements.Contains("Primeira instalação"))
                    config.achievements.Add("Primeira instalação");

                progressBar.Value = 100;
                downloadLabel.Text = "Download: concluído";
                speedLabel.Text = "Velocidade: -";
                etaLabel.Text = "Tempo restante: -";

                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);

                statusLabel.Text = "CONCLUÍDO";
                SaveConfig(config);
                ShowNotification("Instalação concluída", selectedGame.name + " está pronto.", "install-" + selectedGame.id + "-" + selectedGame.version);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "ERRO";
            }
            finally
            {
                busy = false;
                RefreshGamePage();
            }
        }

        private async Task DownloadFileWithProgress(string url, string destination)
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            long totalRead = 0;
            long lastRead = 0;

            DateTime lastSpeedCheck = DateTime.Now;

            using Stream stream = await response.Content.ReadAsStreamAsync();
            using FileStream file = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[81920];
            int read;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await file.WriteAsync(buffer, 0, read);
                totalRead += read;

                if (totalBytes > 0)
                {
                    int percent = (int)((totalRead * 100) / totalBytes);
                    progressBar.Value = Math.Min(percent, 100);
                    downloadLabel.Text = $"Baixando {FormatBytes(totalRead)} / {FormatBytes(totalBytes)} - {percent}%";
                }
                else
                {
                    downloadLabel.Text = $"Baixando {FormatBytes(totalRead)}";
                }

                TimeSpan speedSpan = DateTime.Now - lastSpeedCheck;

                if (speedSpan.TotalSeconds >= 1)
                {
                    long bytesNow = totalRead - lastRead;
                    double speed = bytesNow / speedSpan.TotalSeconds;

                    speedLabel.Text = "Velocidade: " + FormatBytes((long)speed) + "/s";

                    if (totalBytes > 0 && speed > 0)
                    {
                        long remaining = totalBytes - totalRead;
                        etaLabel.Text = "Tempo restante: " + FormatTime(remaining / speed);
                    }

                    lastRead = totalRead;
                    lastSpeedCheck = DateTime.Now;
                }

                Application.DoEvents();
            }
        }

        private string GetInstallPath(GameData game)
        {
            if (config.gameInstallPaths.TryGetValue(game.id, out string? path) && !string.IsNullOrWhiteSpace(path))
                return path;

            string defaultPath = Path.Combine(appData, "Games", game.id);
            config.gameInstallPaths[game.id] = defaultPath;
            SaveConfig(config);

            return defaultPath;
        }

        private void ChangeSelectedGameFolder()
        {
            if (selectedGame == null) return;

            using FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Escolha a pasta onde este jogo será instalado";
            dialog.UseDescriptionForTitle = true;

            if (dialog.ShowDialog() != DialogResult.OK) return;

            config.gameInstallPaths[selectedGame.id] = Path.Combine(dialog.SelectedPath, selectedGame.name);
            SaveConfig(config);
            RefreshGamePage();
        }

        private bool IsGameInstalled(GameData game)
        {
            return File.Exists(Path.Combine(GetInstallPath(game), game.exeName));
        }

        private string ReadLocalGameVersion(GameData game)
        {
            string file = Path.Combine(GetInstallPath(game), game.versionFileName);

            if (!File.Exists(file))
                return "0.0";

            return File.ReadAllText(file).Trim();
        }

        private async void PlaySelectedGame()
        {
            if (selectedGame == null) return;

            GameData game = selectedGame;
            string exe = Path.Combine(GetInstallPath(game), game.exeName);

            if (!File.Exists(exe))
            {
                MessageBox.Show("Jogo não encontrado.");
                return;
            }

            try
            {
                config.lastOnline = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                if (!config.achievements.Contains("Primeiro jogo iniciado"))
                    config.achievements.Add("Primeiro jogo iniciado");

                SaveConfig(config);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = GetInstallPath(game),
                    UseShellExecute = true
                };

                Process? process = Process.Start(startInfo);

                if (process == null)
                {
                    MessageBox.Show("Não foi possível iniciar o jogo.");
                    return;
                }

                DateTime startTime = DateTime.Now;

                statusLabel.Text = "JOGANDO: " + game.name;
                playButton.Enabled = false;

                while (!process.HasExited)
                {
                    await SendHeartbeatAsync("game", game.id);
                    await Task.Delay(15000);
                    process.Refresh();
                }

                DateTime endTime = DateTime.Now;
                TimeSpan playedTime = endTime - startTime;

                double playedHours = Math.Max(0, playedTime.TotalHours);

                if (!config.playedHours.ContainsKey(game.id))
                    config.playedHours[game.id] = 0;

                config.playedHours[game.id] += playedHours;
                config.lastOnline = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                if (playedTime.TotalMinutes >= 1 && !config.achievements.Contains("Jogou por 1 minuto"))
                    config.achievements.Add("Jogou por 1 minuto");

                if (config.playedHours[game.id] >= 1 && !config.achievements.Contains("1 hora jogada"))
                    config.achievements.Add("1 hora jogada");

                SaveConfig(config);

                ShowNotification(
                    "Sessão finalizada",
                    $"{game.name}: {FormatPlayedTime(playedTime)} adicionados ao perfil.",
                    "session-" + game.id + "-" + DateTime.Now.Ticks
                );

                RefreshGamePage();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao iniciar/monitorar o jogo:\n" + ex.Message);
            }
            finally
            {
                if (playButton != null)
                    playButton.Enabled = selectedGame != null && IsGameInstalled(selectedGame);

                statusLabel.Text = "ONLINE";
            }
        }

        private string FormatPlayedTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}h {time.Minutes}min";

            if (time.TotalMinutes >= 1)
                return $"{time.Minutes}min {time.Seconds}s";

            return $"{time.Seconds}s";
        }

        private void OpenSelectedGameFolder()
        {
            if (selectedGame == null) return;

            string path = GetInstallPath(selectedGame);
            Directory.CreateDirectory(path);

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }

        private void CloseGameProcess(GameData game)
        {
            string processName = Path.GetFileNameWithoutExtension(game.exeName);

            foreach (Process p in Process.GetProcessesByName(processName))
            {
                try
                {
                    p.Kill();
                    p.WaitForExit();
                }
                catch { }
            }
        }


        private async Task SendHeartbeatAsync(string place, string? gameId)
        {
            try
            {
                if (onlineData == null)
                    return;

                string url = "";

                if (place == "launcher")
                    url = onlineData.launcherHeartbeatUrl;

                if (place == "game")
                    url = onlineData.gameHeartbeatUrl;

                if (string.IsNullOrWhiteSpace(url))
                    return;

                var payload = new
                {
                    username = config.username,
                    playerName = config.playerName,
                    place = place,
                    gameId = gameId ?? "",
                    launcherVersion = CURRENT_LAUNCHER_VERSION,
                    time = DateTime.UtcNow.ToString("o")
                };

                string json = JsonSerializer.Serialize(payload);

                using HttpClient client = new HttpClient();
                using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(url, content);
            }
            catch
            {
            }
        }

        private async Task UpdateLauncher()
        {
            if (onlineData == null || string.IsNullOrWhiteSpace(onlineData.launcherUpdateUrl)) return;

            try
            {
                string currentExe = Application.ExecutablePath;
                string newExe = Path.Combine(appData, "ZTRCompanyLauncher_new.exe");
                string batFile = Path.Combine(appData, "update_launcher.bat");

                statusLabel.Text = "ATUALIZANDO LAUNCHER";

                using HttpClient client = new HttpClient();
                byte[] data = await client.GetByteArrayAsync(onlineData.launcherUpdateUrl);

                await File.WriteAllBytesAsync(newExe, data);

                if (!string.IsNullOrWhiteSpace(onlineData.launcherSha256))
                {
                    string hash = ComputeSha256(newExe);
                    if (!hash.Equals(onlineData.launcherSha256, StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Hash do launcher não confere. Update cancelado.");
                }

                string bat = $@"
@echo off
timeout /t 2 /nobreak > nul
copy /y ""{newExe}"" ""{currentExe}""
start """" ""{currentExe}""
del ""{newExe}""
del ""%~f0""
";

                File.WriteAllText(batFile, bat);

                Process.Start(new ProcessStartInfo
                {
                    FileName = batFile,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao atualizar launcher:\n" + ex.Message);
            }
        }

        private void ToggleFullscreen()
        {
            if (!isFullScreen)
            {
                oldBounds = Bounds;
                oldBorder = FormBorderStyle;

                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = Screen.FromControl(this).Bounds;

                fullscreenButton.Text = "SAIR TELA";
                isFullScreen = true;
            }
            else
            {
                FormBorderStyle = oldBorder;
                Bounds = oldBounds;

                fullscreenButton.Text = "TELA CHEIA";
                isFullScreen = false;
            }
        }

        private string ComputeSha256(string filePath)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(filePath);

            byte[] hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private void ClearFolder(string path)
        {
            Directory.CreateDirectory(path);

            foreach (string file in Directory.GetFiles(path))
            {
                try { File.Delete(file); } catch { }
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        private void CopyFolder(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (string file in Directory.GetFiles(source))
            {
                string dest = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            foreach (string dir in Directory.GetDirectories(source))
            {
                string dest = Path.Combine(destination, Path.GetFileName(dir));
                CopyFolder(dir, dest);
            }
        }

        private void PlayStartupSound()
        {
            try
            {
                string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ztr_startup_chime.wav");

                if (File.Exists(soundPath))
                {
                    startupSound = new SoundPlayer(soundPath);
                    startupSound.Play();
                }
            }
            catch { }
        }

        private void PlayEnterSound()
        {
            try
            {
                string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ztr_enter_confirm.wav");

                if (File.Exists(soundPath))
                {
                    confirmSound = new SoundPlayer(soundPath);
                    confirmSound.Play();
                }
            }
            catch { }
        }

        private void StartBlink()
        {
            if (!blinkTimer.Enabled)
            {
                blink = false;
                blinkTimer.Start();
            }
        }

        private void StopBlink()
        {
            blinkTimer.Stop();

            if (updateButton != null)
            {
                updateButton.Text = "Atualizar";
                updateButton.NormalColor = Color.FromArgb(42, 116, 255);
                updateButton.HoverColor = Color.FromArgb(70, 145, 255);
                updateButton.PressColor = Color.FromArgb(28, 86, 210);
                updateButton.ApplyVisualState();
            }
        }

        private void BlinkUpdateButton()
        {
            if (updateButton == null || !updateButton.Enabled) return;

            blink = !blink;

            if (blink)
            {
                updateButton.Text = "⚠ Atualizar";
                updateButton.NormalColor = Color.FromArgb(220, 35, 35);
                updateButton.HoverColor = Color.FromArgb(255, 65, 65);
                updateButton.PressColor = Color.FromArgb(170, 20, 20);
                updateButton.ForeColor = Color.White;
            }
            else
            {
                updateButton.Text = "Atualizar";
                updateButton.NormalColor = Color.FromArgb(255, 190, 35);
                updateButton.HoverColor = Color.FromArgb(255, 215, 80);
                updateButton.PressColor = Color.FromArgb(210, 145, 20);
                updateButton.ForeColor = Color.Black;
            }

            updateButton.ApplyVisualState();
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private string FormatTime(double seconds)
        {
            if (seconds < 0 || double.IsInfinity(seconds) || double.IsNaN(seconds))
                return "-";

            TimeSpan t = TimeSpan.FromSeconds(seconds);

            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours}h {t.Minutes}min";

            if (t.TotalMinutes >= 1)
                return $"{t.Minutes}min {t.Seconds}s";

            return $"{t.Seconds}s";
        }

        private LauncherConfig LoadConfig()
        {
            try
            {
                if (File.Exists(configFile))
                {
                    string json = File.ReadAllText(configFile);
                    LauncherConfig? loaded = JsonSerializer.Deserialize<LauncherConfig>(json);

                    if (loaded != null)
                    {
                        loaded.gameInstallPaths ??= new Dictionary<string, string>();
                        loaded.playedHours ??= new Dictionary<string, double>();
                        loaded.achievements ??= new List<string>();
                        return loaded;
                    }
                }
            }
            catch { }

            return new LauncherConfig();
        }

        private void SaveConfig(LauncherConfig data)
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFile, json);
        }

        public class NotifyForm : Form
        {
            private Timer closeTimer = new Timer();

            public NotifyForm(string title, string message)
            {
                Width = 360;
                Height = 125;
                FormBorderStyle = FormBorderStyle.None;
                BackColor = Color.FromArgb(16, 22, 34);
                TopMost = true;
                ShowInTaskbar = false;

                Label titleLabel = new Label
                {
                    Text = title,
                    Left = 18,
                    Top = 15,
                    Width = 320,
                    Height = 28,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold)
                };

                Label msgLabel = new Label
                {
                    Text = message,
                    Left = 18,
                    Top = 48,
                    Width = 320,
                    Height = 55,
                    ForeColor = Color.FromArgb(200, 215, 235),
                    Font = new Font("Segoe UI", 9)
                };

                Controls.Add(titleLabel);
                Controls.Add(msgLabel);

                closeTimer.Interval = 4500;
                closeTimer.Tick += (s, e) =>
                {
                    closeTimer.Stop();
                    Close();
                };
                closeTimer.Start();
            }
        }

        public class ZButton : Button
        {
            public Color NormalColor { get; set; } = Color.FromArgb(42, 116, 255);
            public Color HoverColor { get; set; } = Color.FromArgb(70, 145, 255);
            public Color PressColor { get; set; } = Color.FromArgb(28, 86, 210);
            public Color DisabledColor { get; set; } = Color.FromArgb(45, 50, 60);
            public Color DisabledTextColor { get; set; } = Color.FromArgb(130, 140, 155);

            private bool hovering = false;
            private bool pressing = false;

            public ZButton()
            {
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                Cursor = Cursors.Hand;
                BackColor = NormalColor;
                ForeColor = Color.White;
                SetStyle(ControlStyles.UserPaint, true);
                SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            }

            public void ApplyVisualState()
            {
                if (!Enabled)
                {
                    BackColor = DisabledColor;
                    Cursor = Cursors.Default;
                }
                else if (pressing)
                {
                    BackColor = PressColor;
                    Cursor = Cursors.Hand;
                }
                else if (hovering)
                {
                    BackColor = HoverColor;
                    Cursor = Cursors.Hand;
                }
                else
                {
                    BackColor = NormalColor;
                    Cursor = Cursors.Hand;
                }

                Invalidate();
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                hovering = true;
                ApplyVisualState();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                hovering = false;
                pressing = false;
                ApplyVisualState();
            }

            protected override void OnMouseDown(MouseEventArgs mevent)
            {
                base.OnMouseDown(mevent);
                if (mevent.Button == MouseButtons.Left)
                {
                    pressing = true;
                    ApplyVisualState();
                }
            }

            protected override void OnMouseUp(MouseEventArgs mevent)
            {
                base.OnMouseUp(mevent);
                pressing = false;
                ApplyVisualState();
            }

            protected override void OnEnabledChanged(EventArgs e)
            {
                base.OnEnabledChanged(e);
                pressing = false;
                hovering = false;
                ApplyVisualState();
            }

            protected override void OnPaint(PaintEventArgs pevent)
            {
                Graphics g = pevent.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Parent?.BackColor ?? Color.FromArgb(10, 14, 22));

                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

                using GraphicsPath path = RoundedRect(rect, 12);
                using LinearGradientBrush brush = new LinearGradientBrush(
                    rect,
                    ControlPaint.Light(BackColor, Enabled ? 0.10f : 0.02f),
                    BackColor,
                    LinearGradientMode.Vertical
                );

                g.FillPath(brush, path);

                if (Enabled)
                {
                    using Pen border = new Pen(Color.FromArgb(55, 160, 255), hovering ? 2 : 1);
                    g.DrawPath(border, path);
                }
                else
                {
                    using Pen border = new Pen(Color.FromArgb(58, 64, 76), 1);
                    g.DrawPath(border, path);
                }

                TextRenderer.DrawText(
                    g,
                    Text,
                    Font,
                    ClientRectangle,
                    Enabled ? ForeColor : DisabledTextColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                );
            }

            private GraphicsPath RoundedRect(Rectangle bounds, int radius)
            {
                int d = radius * 2;
                GraphicsPath path = new GraphicsPath();

                path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
                path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
                path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
                path.CloseFigure();

                return path;
            }
        }

        public class LauncherOnlineData
        {
            public string launcherVersion { get; set; } = "";
            public string launcherUpdateUrl { get; set; } = "";
            public string launcherSha256 { get; set; } = "";
            public string serverStatusUrl { get; set; } = "";
            public string launcherHeartbeatUrl { get; set; } = "";
            public string gameHeartbeatUrl { get; set; } = "";
            public List<NewsData> news { get; set; } = new();
            public List<GameData> games { get; set; } = new();
            public List<EventData> events { get; set; } = new();
            public List<ServerData> servers { get; set; } = new();
        }

        public class NewsData
        {
            public string type { get; set; } = "noticia";
            public string title { get; set; } = "";
            public string message { get; set; } = "";
            public string url { get; set; } = "";
        }

        public class EventData
        {
            public string title { get; set; } = "";
            public string message { get; set; } = "";
            public bool active { get; set; } = true;
        }

        public class ServerData
        {
            public string name { get; set; } = "";
            public bool online { get; set; } = false;
            public int pingMs { get; set; } = 0;
            public int playersOnline { get; set; } = 0;
            public int maxPlayers { get; set; } = 0;
        }

        public class GameData
        {
            public string id { get; set; } = "";
            public string name { get; set; } = "";
            public string version { get; set; } = "";
            public string exeName { get; set; } = "";
            public string downloadUrl { get; set; } = "";
            public string sha256 { get; set; } = "";
            public string versionFileName { get; set; } = "game_version.txt";
            public string changelog { get; set; } = "";
            public string iconUrl { get; set; } = "";
            public string bannerUrl { get; set; } = "";
            public bool maintenance { get; set; } = false;
            public bool forceUpdate { get; set; } = true;
        }


        public class ServerStatusData
        {
            public int launcherOnline { get; set; } = 0;
            public int gameOnline { get; set; } = 0;
            public List<HeartbeatUserData> launcherUsers { get; set; } = new();
            public List<HeartbeatUserData> gameUsers { get; set; } = new();
        }

        public class HeartbeatUserData
        {
            public string username { get; set; } = "";
            public string playerName { get; set; } = "";
            public string place { get; set; } = "";
            public string gameId { get; set; } = "";
            public string launcherVersion { get; set; } = "";
            public string time { get; set; } = "";
        }

        public class LauncherConfig
        {
            public string playerName { get; set; } = "Player ZTR";
            public string avatarUrl { get; set; } = "";
            public string lastOnline { get; set; } = "Nunca";
            public Dictionary<string, string> gameInstallPaths { get; set; } = new();
            public Dictionary<string, double> playedHours { get; set; } = new();
            public List<string> achievements { get; set; } = new();
        }
    }
}
