using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Leauge_Auto_Accept
{
    public partial class MainWindow : Window
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private Action renderPage;
        private Action goBack;

        // Pages showing champions are drawn again once the client connects and the data arrives
        private bool redrawOnData;
        private bool lastConnected;
        private bool lastChampsLoaded;
        private bool lastSpellsLoaded;
        private static bool spellsLoading;

        public MainWindow()
        {
            InitializeComponent();

            // Tall enough to show every page without scrolling, unless the screen is smaller than that
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);

            var statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            statusTimer.Tick += (sender, e) => updateStatus();
            statusTimer.Start();

            showHome();
            updateStatus();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            int useDarkMode = 1;
            DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && goBack != null)
            {
                goBack();
                e.Handled = true;
            }
        }

        private void AutoAcceptToggle_Changed(object sender, RoutedEventArgs e)
        {
            // updateStatus also sets this switch, only an actual change should be saved
            if (AutoAcceptToggle.IsChecked != Settings.shouldAutoAcceptbeOn)
            {
                Settings.toggleAutoAcceptSetting();
            }
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e) => showSettings();
        private void NavArena_Click(object sender, RoutedEventArgs e) => showArena();
        private void NavChat_Click(object sender, RoutedEventArgs e) => showChat();
        private void NavInfo_Click(object sender, RoutedEventArgs e) => showInfo();

        private void updateStatus()
        {
            bool connected = LCU.isLeagueOpen && LCU.isAuthReady;
            bool champsLoaded = Data.champsSorted.Count > 0;

            StatusDot.Fill = brush(connected ? "Good" : "Bad");
            StatusText.Foreground = brush(connected ? "Good" : "Bad");
            StatusText.Text = connected ? "Connected" : "League client cannot be found.";
            AutoAcceptToggle.IsChecked = Settings.shouldAutoAcceptbeOn;

            if (connected && LCU.dataLoadFailed)
            {
                BannerText.Text = "Some League data failed to load." + " " + "The app will keep running; try opening the selector again.";
                Banner.Visibility = Visibility.Visible;
            }
            else if (Updater.latestTag != null)
            {
                BannerText.Text = "An update has been found, consider updating." + " "
                    + string.Format("Current version is v{0}, latest version is {1}", Updater.appVersion, Updater.latestTag) + ". "
                    + "You can disable this check in the settings.";
                Banner.Visibility = Visibility.Visible;
            }
            else
            {
                Banner.Visibility = Visibility.Collapsed;
            }

            bool spellsLoaded = Data.spellsSorted.Count > 0;
            if (connected && Settings.useSummonerSpells && !spellsLoaded)
            {
                loadSpellsInBackground();
            }

            if (redrawOnData && (connected != lastConnected || champsLoaded != lastChampsLoaded || spellsLoaded != lastSpellsLoaded))
            {
                renderPage?.Invoke();
            }
            lastConnected = connected;
            lastChampsLoaded = champsLoaded;
            lastSpellsLoaded = spellsLoaded;
        }

        // The spell list is small, so it's fetched once the client connects and the spells are switched on
        private static void loadSpellsInBackground()
        {
            if (spellsLoading)
            {
                return;
            }

            spellsLoading = true;
            Task.Run(() =>
            {
                try
                {
                    Data.loadSpellsList();
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Failed to load summoner spells.");
                }
                finally
                {
                    spellsLoading = false;
                }
            });
        }

        private void applyLabels()
        {
            AutoAcceptLabel.Text = "Auto accept";
            AutomationProperties.SetName(AutoAcceptToggle, "Auto accept");
            NavSettings.Content = "Settings";
            NavArena.Content = "Arena";
            NavChat.Content = "Chat messages";
            NavInfo.Content = "Info";
            VersionText.Text = "v" + Updater.appVersion;
        }

        private void show(Action render, Action back, bool redraws = false)
        {
            renderPage = render;
            goBack = back;
            redrawOnData = redraws;
            render();
        }

        // ---------- Home ----------

        private void showHome()
        {
            show(() =>
            {
                applyLabels();

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());

                var primary = roleCard("Primary role", new[]
                {
                    ("Pick", "Primary Champion", Settings.currentChamp, false, Settings.currentChampRunes),
                    ("Backup pick", "Primary Backup Champion", Settings.currentBackupChamp, false, Settings.currentBackupChampRunes),
                    ("Ban", "Primary Ban", Settings.currentBan, true, null),
                    ("Backup ban", "Primary Backup Ban", Settings.currentBackupBan, true, null),
                });
                primary.Margin = new Thickness(0, 0, 6, 0);
                grid.Children.Add(primary);

                var secondary = roleCard("Secondary role", new[]
                {
                    ("Pick", "Secondary Champion", Settings.secondaryChamp, false, Settings.secondaryChampRunes),
                    ("Backup pick", "Secondary Backup Champion", Settings.secondaryBackupChamp, false, Settings.secondaryBackupChampRunes),
                    ("Ban", "Secondary Ban", Settings.secondaryBan, true, null),
                    ("Backup ban", "Secondary Backup Ban", Settings.secondaryBackupBan, true, null),
                });
                secondary.Margin = new Thickness(6, 0, 0, 0);
                Grid.SetColumn(secondary, 1);
                grid.Children.Add(secondary);

                var home = new StackPanel();
                home.Children.Add(grid);
                if (Settings.useSummonerSpells)
                {
                    var spells = spellsCard();
                    spells.Margin = new Thickness(0, 12, 0, 0);
                    home.Children.Add(spells);
                }

                PageHost.Content = scrolling(home);
            }, null, redraws: true);
        }

        private FrameworkElement roleCard(string title, (string label, string pickerTitle, string[] slot, bool isBan, string[] runes)[] tiles)
        {
            var body = new StackPanel();
            body.Children.Add(new TextBlock { Text = title, FontSize = 13, Foreground = brush("Muted"), Margin = new Thickness(0, 0, 0, 10) });

            foreach (var tile in tiles)
            {
                bool showRunes = tile.runes != null && Settings.useRunePages;
                var slotTile = champTile(tile.label, tile.slot, tile.isBan ? "Bad" : "Gold", () =>
                    showPicker(tile.pickerTitle, tile.slot, tile.isBan, showHome));
                slotTile.Margin = new Thickness(0, 0, 0, showRunes ? 4 : 8);
                body.Children.Add(slotTile);

                if (showRunes)
                {
                    body.Children.Add(runeRow(tile.label, tile.pickerTitle, tile.runes));
                }
            }

            return card(body);
        }

        private Border card(FrameworkElement body)
        {
            return new Border
            {
                BorderBrush = brush("Line"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 6),
                Child = body
            };
        }

        private FrameworkElement runeRow(string label, string pickerTitle, string[] runes)
        {
            bool isSet = runes[1] != "0";

            var name = new TextBlock { Text = "Rune page", FontSize = 12, Foreground = brush("Muted"), Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
            var value = new TextBlock { Text = isSet ? runes[0] : "Choose", Foreground = brush(isSet ? "Gold" : "Dim"), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            var dock = new DockPanel();
            DockPanel.SetDock(name, Dock.Left);
            dock.Children.Add(name);
            dock.Children.Add(value);

            var button = new Button { Style = style("PlainButton"), Content = dock, Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(24, 0, 0, 8) };
            AutomationProperties.SetName(button, label + " rune page: " + value.Text);
            button.Click += (sender, e) => showRunePicker(pickerTitle + " Rune Page", runes);
            return button;
        }

        private FrameworkElement spellsCard()
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition());

            // Same order as in game: spell 1 is on D, spell 2 is on F
            var first = spellTile("Spell 1 (D)", Settings.currentSpell1);
            first.Margin = new Thickness(0, 0, 6, 8);
            row.Children.Add(first);

            var second = spellTile("Spell 2 (F)", Settings.currentSpell2);
            second.Margin = new Thickness(6, 0, 0, 8);
            Grid.SetColumn(second, 1);
            row.Children.Add(second);

            var body = new StackPanel();
            body.Children.Add(new TextBlock { Text = "Summoner spells", FontSize = 13, Foreground = brush("Muted"), Margin = new Thickness(0, 0, 0, 10) });
            body.Children.Add(row);
            return card(body);
        }

        private Button spellTile(string label, string[] slot)
        {
            bool isSet = slot[1] != "0";
            var spell = Data.spellsSorted.FirstOrDefault(s => s.name == slot[0]);
            return slotTile(label, isSet ? slot[0] : "Choose", isSet, isSet ? slot[0].Substring(0, 1) : "+",
                spell != null ? Icons.Spell(spell) : null, "Accent", () => showSpellPicker(label, slot));
        }

        private Button champTile(string label, string[] slot, string accent, Action onClick)
        {
            bool isSet = slot[1] != "0";
            bool isChampion = int.TryParse(slot[1], out int id) && id > 0;
            return slotTile(label, isSet ? slot[0] : "Choose", isSet, isChampion ? slot[0].Substring(0, 1) : "+",
                isChampion ? Icons.Champion(slot[1]) : null, accent, onClick);
        }

        private Button slotTile(string label, string text, bool isSet, string letter, Task<ImageSource> icon, string accent, Action onClick)
        {
            var words = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
            words.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = brush("Muted") });
            words.Children.Add(new TextBlock { Text = text, FontSize = 15, Foreground = brush(isSet ? "Text" : "Dim") });

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(portrait(letter, icon, 44, isSet ? accent : "Line"));
            row.Children.Add(words);

            var button = new Button { Style = style("PlainButton"), Content = row, Padding = new Thickness(8) };
            AutomationProperties.SetName(button, label + ": " + text);
            button.Click += (sender, e) => onClick();
            return button;
        }

        private FrameworkElement portrait(string letterText, Task<ImageSource> icon, double size, string accent)
        {
            var letter = new TextBlock
            {
                Text = letterText,
                FontSize = size * 0.4,
                Foreground = brush(accent),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var image = new Image { Width = size, Height = size, Stretch = Stretch.UniformToFill };
            var content = new Grid { ClipToBounds = true };
            content.Children.Add(letter);
            content.Children.Add(image);

            if (icon != null)
            {
                loadPortrait(image, letter, icon);
            }

            return new Border
            {
                Width = size,
                Height = size,
                BorderBrush = brush(accent),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Background = brush("Bg"),
                Child = content
            };
        }

        private static async void loadPortrait(Image image, TextBlock letter, Task<ImageSource> icon)
        {
            var source = await icon;
            if (source != null)
            {
                image.Source = source;
                letter.Visibility = Visibility.Collapsed;
            }
        }

        // ---------- Champion picker ----------

        private void showPicker(string title, string[] slot, bool isBan, Action back)
        {
            show(() =>
            {
                var page = pageFrame(title, back);

                if (!Data.champsSorted.Any())
                {
                    if (!LCU.isLeagueOpen || !LCU.isAuthReady)
                    {
                        page.Children.Add(message("Open the League client to choose a champion."));
                    }
                    else
                    {
                        page.Children.Add(message("Getting champions and ownership list..."));
                        loadThenDraw(page, Data.loadChampionsList, "Failed to load champions from League.");
                    }
                    PageHost.Content = page;
                    return;
                }

                var options = new List<(string name, string id)>
                {
                    ("Unselected", "0")
                };
                if (isBan)
                {
                    options.Add(("None", "-1"));
                }
                options.AddRange(Data.champsSorted
                    .Where(champ => champ.free || isBan)
                    .Select(champ => (champ.name, champ.id)));

                var grid = new WrapPanel();
                var tiles = new List<(string search, Button button)>();
                foreach (var option in options)
                {
                    bool isChampion = int.Parse(option.id) > 0;
                    var tile = pickerTile(option.name, isChampion ? option.name.Substring(0, 1) : "+",
                        isChampion ? Icons.Champion(option.id) : null, slot[1] == option.id);
                    tile.Click += (sender, e) =>
                    {
                        Settings.saveSelection(slot, option.name, option.id);
                        back();
                    };
                    grid.Children.Add(tile);
                    tiles.Add((option.name, tile));
                }

                var search = new TextBox { Margin = new Thickness(0, 0, 0, 4) };
                AutomationProperties.SetName(search, "Search");
                search.TextChanged += (sender, e) =>
                {
                    foreach (var tile in tiles)
                    {
                        bool matches = tile.search.Contains(search.Text, StringComparison.OrdinalIgnoreCase);
                        tile.button.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
                search.KeyDown += (sender, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        var first = tiles.FirstOrDefault(tile => tile.button.Visibility == Visibility.Visible).button;
                        first?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                };
                addTop(page, search);
                addTop(page, new TextBlock { Text = "Type to search, press Enter to choose the first match", FontSize = 12, Foreground = brush("Dim"), Margin = new Thickness(0, 0, 0, 8) });
                page.Children.Add(scrolling(grid));

                PageHost.Content = page;
                Dispatcher.BeginInvoke(() => search.Focus(), DispatcherPriority.Input);
            }, back, redraws: true);
        }

        private async void loadThenDraw(Panel body, Func<bool> load, string failure)
        {
            bool loaded = false;
            try
            {
                loaded = await Task.Run(load);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, failure);
            }

            if (loaded)
            {
                renderPage?.Invoke();
            }
            else if (body.IsLoaded)
            {
                body.Children.RemoveAt(body.Children.Count - 1);
                body.Children.Add(message(failure));
            }
        }

        private Button pickerTile(string name, string letter, Task<ImageSource> icon, bool selected)
        {
            var column = new StackPanel { Width = 76 };
            var image = portrait(letter, icon, 52, selected ? "Gold" : "Line");
            image.HorizontalAlignment = HorizontalAlignment.Center;
            column.Children.Add(image);
            column.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 11,
                Foreground = brush(selected ? "Gold" : "Text"),
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 4, 0, 0)
            });

            var button = new Button
            {
                Style = style("PlainButton"),
                Content = column,
                Padding = new Thickness(4, 6, 4, 4),
                Margin = new Thickness(0, 0, 6, 6),
                BorderBrush = brush(selected ? "Gold" : "Line")
            };
            AutomationProperties.SetName(button, name);
            return button;
        }

        // ---------- Summoner spell and rune page pickers ----------

        private void showSpellPicker(string title, string[] slot)
        {
            show(() =>
            {
                var page = pageFrame(title, showHome);

                if (!Data.spellsSorted.Any())
                {
                    if (!LCU.isLeagueOpen || !LCU.isAuthReady)
                    {
                        page.Children.Add(message("Open the League client to choose a summoner spell."));
                    }
                    else
                    {
                        page.Children.Add(message("Getting a list of summoner spells..."));
                        loadThenDraw(page, Data.loadSpellsList, "Failed to load summoner spells from League.");
                    }
                    PageHost.Content = page;
                    return;
                }

                // Some game modes have their own copy of a spell, one entry per name is enough here
                var grid = new WrapPanel();
                grid.Children.Add(spellOption("Unselected", "0", null, slot));
                foreach (var spell in Data.spellsSorted.GroupBy(s => s.name).Select(g => g.First()).OrderBy(s => s.name))
                {
                    grid.Children.Add(spellOption(spell.name, spell.id, spell, slot));
                }

                page.Children.Add(scrolling(grid));
                PageHost.Content = page;
            }, showHome, redraws: true);
        }

        private Button spellOption(string name, string id, spellList spell, string[] slot)
        {
            bool selected = spell == null ? slot[1] == "0" : slot[1] != "0" && slot[0] == name;
            var tile = pickerTile(name, spell == null ? "+" : name.Substring(0, 1), spell == null ? null : Icons.Spell(spell), selected);
            tile.Click += (sender, e) =>
            {
                Settings.saveSelection(slot, name, id);
                showHome();
            };
            return tile;
        }

        private void showRunePicker(string title, string[] slot)
        {
            bool loaded = false;
            show(() =>
            {
                var page = pageFrame(title, showHome);

                if (!LCU.isLeagueOpen || !LCU.isAuthReady)
                {
                    page.Children.Add(message("Open the League client to choose a rune page."));
                    PageHost.Content = page;
                    return;
                }

                if (!loaded)
                {
                    page.Children.Add(message("Getting your rune pages..."));
                    PageHost.Content = page;
                    loadThenDraw(page, () => loaded = Data.loadRunesList(), "Failed to load your rune pages from League.");
                    return;
                }

                var rows = new StackPanel();
                rows.Children.Add(runePageRow(null, slot));

                var mobalytics = Data.runesList.Where(runes => runes.fromMobalytics).ToList();
                var own = Data.runesList.Where(runes => !runes.fromMobalytics).ToList();
                if (mobalytics.Any())
                {
                    rows.Children.Add(sectionTitle("From Mobalytics"));
                    mobalytics.ForEach(runes => rows.Children.Add(runePageRow(runes, slot)));
                }
                if (own.Any())
                {
                    rows.Children.Add(sectionTitle("Your pages"));
                    own.ForEach(runes => rows.Children.Add(runePageRow(runes, slot)));
                }
                rows.Children.Add(message("Mobalytics pages appear here after the Mobalytics app saves them to your League client."));

                page.Children.Add(scrolling(rows));
                PageHost.Content = page;
            }, showHome, redraws: true);
        }

        // Shows the keystone and both trees so similar pages can be told apart
        private Button runePageRow(runePage runes, string[] slot)
        {
            string name = runes?.name ?? "Unselected";
            string id = runes?.id ?? "0";
            bool selected = slot[1] == id;

            var words = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
            words.Children.Add(new TextBlock { Text = name, Foreground = brush(selected ? "Gold" : (runes == null ? "Muted" : "Text")), TextTrimming = TextTrimming.CharacterEllipsis });
            if (runes != null)
            {
                string trees = string.Join(" + ", new[] { runes.primaryStyleName, runes.secondaryStyleName }.Where(tree => !string.IsNullOrEmpty(tree)));
                string details = string.Join("  ·  ", new[] { runes.keystoneName, trees }.Where(part => !string.IsNullOrEmpty(part)));
                words.Children.Add(new TextBlock { Text = details, FontSize = 12, Foreground = brush("Muted"), TextTrimming = TextTrimming.CharacterEllipsis });
            }

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(portrait(runes == null ? "–" : "", runes == null ? null : Icons.FromPath(runes.keystoneIconPath), 36, selected ? "Gold" : "Line"));
            row.Children.Add(words);

            var button = new Button
            {
                Style = style("PlainButton"),
                Content = row,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = brush(selected ? "Gold" : "Line")
            };
            AutomationProperties.SetName(button, name);
            button.Click += (sender, e) =>
            {
                Settings.saveSelection(slot, name, id);
                if (runes != null)
                {
                    // Make it the active page right away, champ select sets it again for the champion that gets picked
                    Task.Run(() => Data.setCurrentRunePage(id));
                }
                showHome();
            };
            return button;
        }

        private TextBlock sectionTitle(string text)
        {
            return new TextBlock { Text = text, FontSize = 13, Foreground = brush("Gold"), Margin = new Thickness(0, 8, 0, 8) };
        }

        // ---------- Settings ----------

        private void showSettings()
        {
            show(() =>
            {
                var page = pageFrame("Settings", showHome);
                var rows = new StackPanel();

                (string name, string[] desc, bool value)[] toggles =
                {
                    ("Save settings/config", new[] { "Save settings for the next time you open the app.", "This will create a settings file in the %AppData% folder." }, Settings.saveSettings),
                    ("Preload data", new[] { "Preload all data the app will need on launch.", "This includes the champions list and more." }, Settings.preloadData),
                    ("Instalock pick", new[] { "Instantly lock in when it's your turn to pick.", "This will bypass the lock in delay setting." }, Settings.instaLock),
                    ("Instalock ban", new[] { "Instantly lock in when it's your turn to ban.", "This will bypass the lock in delay setting." }, Settings.instaBan),
                    ("Disable update check", new[] { "Disable update check on startup." }, Settings.disableUpdateCheck),
                    ("Automatically trade pick order", new[] { "Automatically trade pick order when someone requests to." }, Settings.autoPickOrderTrade),
                    ("Instantly hover pick", new[] { "Instantly hover champion as soon as joining champ select.", "In draft pick, it will hover before you are normally able to." }, Settings.instantHover),
                    ("Automatically restart queue", new[] { "Automatically restart queue every few minutes.", "Default is 5 minutes, can be configured in the delays settings." }, Settings.autoRestartQueue),
                    ("Cancel queue after dodge", new[] { "Automatically cancel the queue after someone dodges the lobby." }, Settings.cancelQueueAfterDodge),
                    ("Rune pages", new[] { "Choose a rune page for each champion pick and set it in champ select.", "Leave this off if another app, such as Mobalytics, sets your runes." }, Settings.useRunePages),
                    ("Summoner spells", new[] { "Choose two summoner spells and set them in champ select.", "Leave this off if another app sets your spells." }, Settings.useSummonerSpells),
                };

                for (int i = 0; i < toggles.Length; i++)
                {
                    int item = i;
                    rows.Children.Add(toggleRow(toggles[i].name, toggles[i].desc, toggles[i].value, () =>
                    {
                        Settings.settingsModify(item);
                        renderPage();
                    }));
                }

                rows.Children.Add(linkRow("Delay settings", "Adjust different delays.", "", showDelays));

                page.Children.Add(scrolling(rows));
                PageHost.Content = page;
            }, showHome);
        }

        private void showDelays()
        {
            show(() =>
            {
                var page = pageFrame("Delay settings", showSettings);
                var rows = new StackPanel();

                (string name, string[] desc, int value)[] delays =
                {
                    ("Pick hover delay upon phase start", new[] { "Seconds to wait before hovering your pick.", "Default is 10." }, Settings.pickStartHoverDelay),
                    ("Pick lock delay upon phase start", new[] { "Seconds to wait before locking in your pick, once you're able to.", "Leave empty to never lock in early. Default is never." }, Settings.pickStartlockDelay),
                    ("Pick lock delay before phase end", new[] { "Seconds before your time runs out to lock in.", "Below 0.3 can make you dodge. Default is 1." }, Settings.pickEndlockDelay),
                    ("Ban hover delay upon phase start", new[] { "Seconds to wait before hovering your ban.", "Default is 1.5." }, Settings.banStartHoverDelay),
                    ("Ban lock delay upon phase start", new[] { "Seconds to wait before locking in your ban, once you're able to.", "Leave empty to never lock in early. Default is never." }, Settings.banStartlockDelay),
                    ("Ban lock delay before phase end", new[] { "Seconds before your time runs out to lock in.", "Default is 1." }, Settings.banEndlockDelay),
                    ("Max queue time before restart", new[] { "Seconds to queue before cancelling and restarting it.", "Default is 300 (5 minutes)." }, Settings.queueMaxTime),
                    ("Chat Messages Delay", new[] { "Seconds to wait before sending your chat messages.", "Default is 0.1." }, Settings.chatMessagesDelay),
                };

                for (int i = 0; i < delays.Length; i++)
                {
                    rows.Children.Add(delayRow(i, delays[i].name, delays[i].desc, delays[i].value));
                }

                page.Children.Add(scrolling(rows));
                PageHost.Content = page;
            }, showSettings);
        }

        // Delays are kept in milliseconds but shown in seconds, the largest value means never
        private const int neverDelay = 999999999;

        private static string toSeconds(int milliseconds)
        {
            return milliseconds >= neverDelay ? "" : (milliseconds / 1000.0).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private FrameworkElement delayRow(int item, string name, string[] desc, int value)
        {
            var input = new TextBox { Text = toSeconds(value), Width = 90, TextAlignment = TextAlignment.Right, MaxLength = 9, VerticalAlignment = VerticalAlignment.Center };
            AutomationProperties.SetName(input, name);
            input.PreviewTextInput += (sender, e) => e.Handled = !e.Text.All(c => char.IsDigit(c) || c == '.');
            DataObject.AddPastingHandler(input, (sender, e) => e.CancelCommand());
            Action commit = () =>
            {
                string text = input.Text.Trim();
                if (text.Length == 0)
                {
                    value = neverDelay;
                    Settings.setDelay(item, value);
                }
                else if (double.TryParse(text, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out double seconds))
                {
                    value = (int)Math.Min(Math.Round(seconds * 1000), neverDelay);
                    Settings.setDelay(item, value);
                }
                input.Text = toSeconds(value);
            };
            input.LostFocus += (sender, e) => commit();
            input.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    commit();
                }
            };

            var field = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            field.Children.Add(input);
            field.Children.Add(new TextBlock { Text = "sec", Foreground = brush("Muted"), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            return settingRow(name, desc, field);
        }

        // ---------- Arena ----------

        private void showArena()
        {
            show(() =>
            {
                var page = pageFrame("Arena", showHome);
                var rows = new StackPanel();

                rows.Children.Add(toggleRow("Enable Bravery", new[] { "Enable or disable bravery for arena games.", "This will pick bravery in arena games over your selected champion" }, Settings.bravery, () =>
                {
                    Settings.toggleBraverySetting();
                    renderPage();
                }));
                rows.Children.Add(toggleRow("Ban crowd favourite champion", new[] { "Enable or disable banning one of the selected crowd favourite champion", "If true and the ban matches one of the crowd favourite champions, It will ban None." }, Settings.banCrowdFavourite, () =>
                {
                    Settings.toggleBanCrowdFavouriteSetting();
                    renderPage();
                }));

                (string name, string[] slot)[] favourites =
                {
                    ("Crowd favourite 1st", Settings.crowdFavouraiteChamp1),
                    ("Crowd favourite 2nd", Settings.crowdFavouraiteChamp2),
                    ("Crowd favourite 3rd", Settings.crowdFavouraiteChamp3),
                    ("Crowd favourite 4th", Settings.crowdFavouraiteChamp4),
                    ("Crowd favourite 5th", Settings.crowdFavouraiteChamp5),
                };
                rows.Children.Add(new TextBlock { Text = "This will be picked over bravery.", FontSize = 12, Foreground = brush("Muted"), Margin = new Thickness(0, 8, 0, 8) });
                foreach (var favourite in favourites)
                {
                    var tile = champTile(favourite.name, favourite.slot, "Gold", () =>
                        showPicker(favourite.name, favourite.slot, false, showArena));
                    tile.Margin = new Thickness(0, 0, 0, 8);
                    rows.Children.Add(tile);
                }

                page.Children.Add(scrolling(rows));
                PageHost.Content = page;
            }, showHome, redraws: true);
        }

        // ---------- Chat messages ----------

        private void showChat()
        {
            show(() =>
            {
                var page = pageFrame("Chat messages", showHome);
                var rows = new StackPanel();

                for (int i = 0; i < Settings.chatMessages.Count; i++)
                {
                    int index = i;
                    var row = new Button
                    {
                        Style = style("PlainButton"),
                        Margin = new Thickness(0, 0, 0, 8),
                        Content = new TextBlock { Text = Settings.chatMessages[i], TextTrimming = TextTrimming.CharacterEllipsis }
                    };
                    AutomationProperties.SetName(row, Settings.chatMessages[i]);
                    row.Click += (sender, e) => showChatEdit(index);
                    rows.Children.Add(row);
                }

                var add = new Button { Style = style("PlainButton"), Content = "Add message", Foreground = brush("Gold") };
                add.Click += (sender, e) => showChatEdit(Settings.chatMessages.Count);
                rows.Children.Add(add);

                page.Children.Add(scrolling(rows));
                PageHost.Content = page;
            }, showHome);
        }

        private void showChatEdit(int index)
        {
            show(() =>
            {
                var page = pageFrame("Chat messages", showChat);

                var input = new TextBox
                {
                    MaxLength = 200,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = false,
                    MinHeight = 80,
                    Margin = new Thickness(0, 0, 0, 12),
                    Text = index < Settings.chatMessages.Count ? Settings.chatMessages[index] : ""
                };
                AutomationProperties.SetName(input, "Message");
                addTop(page, input);

                var buttons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
                buttons.Children.Add(actionButton("Save", () =>
                {
                    if (input.Text.Length == 0)
                    {
                        Settings.deleteChatMessage(index);
                    }
                    else
                    {
                        Settings.updateChatMessage(index, input.Text);
                    }
                    showChat();
                }));
                buttons.Children.Add(actionButton("Delete", () =>
                {
                    Settings.deleteChatMessage(index);
                    showChat();
                }));
                buttons.Children.Add(actionButton("Cancel", showChat));
                page.Children.Add(buttons);

                PageHost.Content = page;
                Dispatcher.BeginInvoke(() => input.Focus(), DispatcherPriority.Input);
            }, showChat);
        }

        // ---------- Info ----------

        private void showInfo()
        {
            show(() =>
            {
                var page = pageFrame("Info", showHome);
                var rows = new StackPanel();

                rows.Children.Add(infoLine("Made by", "sweetriverfish"));
                rows.Children.Add(infoLine("Version", Updater.appVersion));

                rows.Children.Add(new TextBlock { Text = "Source code:", Foreground = brush("Gold"), Margin = new Thickness(0, 16, 0, 2) });
                var source = new Button { Style = style("LinkButton"), Content = "github.com/sweetriverfish/LeagueAutoAccept", Foreground = brush("Accent"), HorizontalAlignment = HorizontalAlignment.Left };
                source.Click += (sender, e) => Process.Start(new ProcessStartInfo("https://github.com/sweetriverfish/LeagueAutoAccept") { UseShellExecute = true });
                rows.Children.Add(source);

                rows.Children.Add(new TextBlock { Text = "Contributors:", Foreground = brush("Gold"), Margin = new Thickness(0, 16, 0, 4) });
                string[] contributors = { "IxPrumxI", "ivelmiskin3", "mrmark1998", "mrcyclo", "considerate-mouth", "DanielBiondi", "ericnewton76" };
                foreach (var contributor in contributors.OrderBy(_ => Random.Shared.Next()))
                {
                    rows.Children.Add(new TextBlock { Text = contributor, Foreground = brush("Muted"), FontSize = 13 });
                }

                page.Children.Add(scrolling(rows));
                PageHost.Content = page;
            }, showHome);
        }

        // ---------- Shared pieces ----------

        // Items are added from the top with addTop, the last item fills what's left of the window
        private DockPanel pageFrame(string title, Action back)
        {
            var body = new DockPanel();
            var heading = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
            var backButton = new Button { Style = style("LinkButton"), Content = "← " + "Back", Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
            backButton.Click += (sender, e) => back();
            heading.Children.Add(backButton);
            heading.Children.Add(new TextBlock { Text = title, FontSize = 17, Foreground = brush("Text"), VerticalAlignment = VerticalAlignment.Center });
            addTop(body, heading);
            return body;
        }

        private static void addTop(DockPanel page, UIElement item)
        {
            DockPanel.SetDock(item, Dock.Top);
            page.Children.Add(item);
        }

        private FrameworkElement scrolling(FrameworkElement content)
        {
            return new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false, Padding = new Thickness(0, 0, 10, 0) };
        }

        private FrameworkElement toggleRow(string name, string[] desc, bool value, Action onToggle)
        {
            var toggle = new ToggleButton { Style = style("Switch"), IsChecked = value };
            AutomationProperties.SetName(toggle, name);
            toggle.Checked += (sender, e) => onToggle();
            toggle.Unchecked += (sender, e) => onToggle();
            return settingRow(name, desc, toggle);
        }

        private FrameworkElement linkRow(string name, string desc, string value, Action onClick)
        {
            var dock = new DockPanel();
            var right = new TextBlock { Text = value + "  ›", Foreground = brush("Gold"), VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(right, Dock.Right);
            dock.Children.Add(right);
            dock.Children.Add(rowText(name, new[] { desc }));

            var button = new Button { Style = style("PlainButton"), Content = dock, Margin = new Thickness(0, 0, 0, 8) };
            AutomationProperties.SetName(button, name);
            button.Click += (sender, e) => onClick();
            return button;
        }

        private FrameworkElement settingRow(string name, string[] desc, FrameworkElement control)
        {
            var dock = new DockPanel();
            DockPanel.SetDock(control, Dock.Right);
            control.Margin = new Thickness(16, 0, 0, 0);
            dock.Children.Add(control);
            dock.Children.Add(rowText(name, desc));

            return new Border
            {
                BorderBrush = brush("Line"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 8),
                Child = dock
            };
        }

        private FrameworkElement rowText(string name, string[] desc)
        {
            var text = new StackPanel();
            text.Children.Add(new TextBlock { Text = name });
            text.Children.Add(new TextBlock { Text = string.Join(" ", desc), FontSize = 12, Foreground = brush("Muted"), TextWrapping = TextWrapping.Wrap });
            return text;
        }

        private FrameworkElement infoLine(string name, string value)
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            line.Children.Add(new TextBlock { Text = name + ":", Foreground = brush("Muted"), Margin = new Thickness(0, 0, 8, 0) });
            line.Children.Add(new TextBlock { Text = value });
            return line;
        }

        private Button actionButton(string text, Action onClick)
        {
            var button = new Button { Style = style("PlainButton"), Content = text, Margin = new Thickness(0, 0, 8, 0), MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
            button.Click += (sender, e) => onClick();
            return button;
        }

        private TextBlock message(string text)
        {
            return new TextBlock { Text = text, Foreground = brush("Muted"), Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap };
        }

        private Brush brush(string key) => (Brush)FindResource(key);

        private Style style(string key) => (Style)FindResource(key);
    }
}
