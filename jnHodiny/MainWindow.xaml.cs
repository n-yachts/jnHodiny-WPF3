using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using jnHodiny.Audio;
using jnHodiny.Display;
using jnHodiny.Ringing;
using jnHodiny.Settings;
using jnHodiny.Snapping;
using jnHodiny.Tray;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace jnHodiny
{
    /// <summary>
    /// Okno hodin. Řeší jen zobrazení a ovládání (myš, menu); logiku nastavení, rozvrhu zvonění,
    /// přichycení, zvuku a ikony v oznamovací oblasti obstarávají samostatné třídy.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double SnapDistance = 40;
        private const double ColorStep = 10;
        private const double FontSizeStep = 2;
        private const double OpacityStep = 0.05;
        private const double CornerRadiusStep = 5;

        private readonly RegistrySettingsStore _store = new();
        private readonly RingSchedule _schedule;
        private readonly BellPlayer _bells;
        private readonly TrayIcon _tray;
        private readonly DispatcherTimer _timer;

        private AppSettings _settings;

        private SnapState _snap = SnapState.None;
        private bool _snapping;          // okno právě měníme sami, událost LocationChanged se ignoruje
        private bool _updatingMenu;      // zaškrtnutí v menu měníme sami, Option_Changed se ignoruje
        private bool _dragging;
        private bool _exiting;
        private Point _dragStart;

        private TimeSpan? _lastTick;     // čas předchozího tiku - podle něj se pozná překročení času zvonění
        private int _lastSecond = -1;
        private bool _blinkOn = true;
        private string _lastMaskKey = "";

        public MainWindow()
        {
            InitializeComponent();

            _settings = _store.Load();
            _schedule = RingSchedule.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "ringing.txt"));
            _bells = new BellPlayer(AppContext.BaseDirectory);

            RestorePosition();
            ApplySettings();

            _tray = new TrayIcon(Title, MainContextMenu, ToggleWindowVisibility);

            // Při odhlášení nebo vypnutí Windows se uloží nastavení a okno se nesmí bránit ukončení
            Application.Current.SessionEnding += (s, e) =>
            {
                _exiting = true;
                SaveSettings();
            };

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += (s, e) => UpdateClock();
            _timer.Start();
        }

        // ---------------------------------------------------------------------------------------
        // Nastavení: stav v AppSettings -> menu a vzhled okna
        // ---------------------------------------------------------------------------------------

        // Promítne celé nastavení do menu, vzhledu a zobrazeného času
        private void ApplySettings()
        {
            SyncMenuFromSettings();
            ApplyAppearance();
            UpdateMenuVisibility();
            UpdateClock();
        }

        private void ApplyAppearance()
        {
            TimeTextBlock.FontSize = _settings.FontSize;
            var textBrush = new SolidColorBrush(_settings.TransparentText ? Colors.Transparent : _settings.TextColor);
            TimeTextBlock.Foreground = textBrush;
            BellTextBlock.Foreground = textBrush;
            FiveTextBlock.Foreground = textBrush;
            BellTextBlock.FontSize = _settings.FontSize * 0.45;
            FiveTextBlock.FontSize = _settings.FontSize * 0.35;
            MainBorder.Background = new SolidColorBrush(_settings.TransparentBackground ? Colors.Transparent : _settings.BackgroundColor);
            Opacity = _settings.Opacity;
            _lastMaskKey = ""; // vzhled se změnil, maska se musí přepočítat
        }

        private void SyncMenuFromSettings()
        {
            _updatingMenu = true;
            TimeFormatMenuItem.IsChecked = _settings.ShortTimeFormat;
            Time24hMenuItem.IsChecked = _settings.Use24HourFormat;
            BellMenuItem.IsChecked = _settings.BellEnabled;
            FiveMinuteMenuItem.IsChecked = _settings.FiveMinuteWarning;
            CountdownBellMenuItem.IsChecked = _settings.CountdownEnabled;
            TransparentBackgroundMenuItem.IsChecked = _settings.TransparentBackground;
            TransparentTextMenuItem.IsChecked = _settings.TransparentText;
            _updatingMenu = false;
        }

        private void ReadMenuIntoSettings()
        {
            _settings.ShortTimeFormat = TimeFormatMenuItem.IsChecked;
            _settings.Use24HourFormat = Time24hMenuItem.IsChecked;
            _settings.BellEnabled = BellMenuItem.IsChecked;
            _settings.FiveMinuteWarning = FiveMinuteMenuItem.IsChecked;
            _settings.CountdownEnabled = CountdownBellMenuItem.IsChecked;
            _settings.TransparentBackground = TransparentBackgroundMenuItem.IsChecked;
            _settings.TransparentText = TransparentTextMenuItem.IsChecked;
        }

        // Zvonění, odpočet a upozornění se nabízejí jen s rozvrhem; "5 minut" navíc jen při zapnutém zvonění
        private void UpdateMenuVisibility()
        {
            bool hasSchedule = !_schedule.IsEmpty;
            BellMenuItem.Visibility = hasSchedule ? Visibility.Visible : Visibility.Collapsed;
            CountdownBellMenuItem.Visibility = BellMenuItem.Visibility;
            FiveMinuteMenuItem.Visibility = hasSchedule && _settings.BellEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        // Poloha okna se omezí na oblast všech monitorů (po odpojení monitoru by okno zůstalo mimo obrazovku)
        private void RestorePosition()
        {
            double maxLeft = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 50;
            double maxTop = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 50;
            Left = Math.Max(SystemParameters.VirtualScreenLeft, Math.Min(maxLeft, _settings.Left));
            Top = Math.Max(SystemParameters.VirtualScreenTop, Math.Min(maxTop, _settings.Top));
        }

        // Při chybě zápisu do registru se nic nezobrazuje, jen při ukončení (showError)
        private void SaveSettings(bool showError = false)
        {
            _settings.Left = Left;
            _settings.Top = Top;

            try
            {
                _store.Save(_settings);
            }
            catch (Exception ex)
            {
                if (showError)
                {
                    MessageBox.Show($"Chyba při ukládání parametrů do registru: {ex.Message}", "Chyba",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ---------------------------------------------------------------------------------------
        // Čas, zvonění a velikost okna
        // ---------------------------------------------------------------------------------------

        private void UpdateClock()
        {
            DateTime now = DateTime.Now;
            TimeSpan timeOfDay = now.TimeOfDay;

            string text = ClockFormatter.FormatTime(now, _settings.Use24HourFormat, _settings.ShortTimeFormat);

            if (!_schedule.IsEmpty)
            {
                if (_settings.CountdownEnabled)
                {
                    text = ClockFormatter.FormatCountdown(_schedule.TimeUntilNext(timeOfDay), _settings.ShortTimeFormat);
                }

                if (_settings.BellEnabled && _lastTick.HasValue)
                {
                    if (_schedule.RingCrossed(_lastTick.Value, timeOfDay))
                    {
                        _bells.PlayBell();
                    }
                    else if (_settings.FiveMinuteWarning && _schedule.WarningCrossed(_lastTick.Value, timeOfDay))
                    {
                        _bells.PlayWarning();
                    }
                }
            }
            _lastTick = timeOfDay;

            // Ve zkráceném formátu dvojtečky blikají jednou za sekundu
            if (_settings.ShortTimeFormat)
            {
                if (now.Second != _lastSecond)
                {
                    _blinkOn = !_blinkOn;
                    _lastSecond = now.Second;
                }
                if (!_blinkOn) text = ClockFormatter.WithoutColons(text);
            }

            TimeTextBlock.Text = text;
            UpdateBellIndicator();
            UpdateWindowSize();
            UpdateTextMask();
        }

        // Zvonek se zobrazuje při zapnutém zvonění, "-5" pod ním při zapnutém upozornění 5 minut
        private void UpdateBellIndicator()
        {
            bool bell = _settings.BellEnabled && !_schedule.IsEmpty;
            BellPanel.Visibility = bell ? Visibility.Visible : Visibility.Collapsed;
            FiveTextBlock.Visibility = bell && _settings.FiveMinuteWarning ? Visibility.Visible : Visibility.Collapsed;
        }

        private Typeface CurrentTypeface => new(TimeTextBlock.FontFamily, TimeTextBlock.FontStyle,
            TimeTextBlock.FontWeight, TimeTextBlock.FontStretch);

        private void UpdateWindowSize()
        {
            var formatted = new FormattedText(
                TimeTextBlock.Text,
                CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                CurrentTypeface,
                TimeTextBlock.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Zaoblené rohy ubírají okraji místo, proto se šířka s poloměrem zvětšuje
            BellPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            bool bell = BellPanel.Visibility == Visibility.Visible;
            double bellWidth = bell ? BellPanel.DesiredSize.Width + BellPanel.Margin.Left + BellPanel.Margin.Right : 0;
            double width = formatted.Width * ((_settings.CornerRadius / 7 + 100) / 100) + 10 + bellWidth;
            double height = Math.Max(formatted.Height, bell ? BellPanel.DesiredSize.Height : 0) + 20;

            if (Math.Abs(width - ActualWidth) > 0.5 || Math.Abs(height - ActualHeight) > 0.5)
            {
                var bounds = ScreenSnapper.Resize(
                    _snap,
                    ScreenHelper.GetWorkArea(this),
                    new Rect(Left, Top, ActualWidth, ActualHeight),
                    new Size(width, height));

                _snapping = true;
                Width = bounds.Width;
                Height = bounds.Height;
                Left = bounds.Left;
                Top = bounds.Top;
                _snapping = false;
            }

            // Poloměr zaoblení se zadává v procentech výšky okna
            MainBorder.CornerRadius = new CornerRadius(_settings.CornerRadius / 100.0 * height);
        }

        // Maska se přepočítá jen při změně textu, písma, velikosti nebo zaoblení
        private void UpdateTextMask()
        {
            if (!_settings.TransparentText || ActualWidth == 0 || ActualHeight == 0)
            {
                OpacityMask = null;
                _lastMaskKey = "";
                return;
            }

            string key = $"{TimeTextBlock.Text}|{BellPanel.Visibility}|{FiveTextBlock.Visibility}|{TimeTextBlock.FontSize}|{ActualWidth}|{ActualHeight}|{MainBorder.CornerRadius.TopLeft}";
            if (key == _lastMaskKey && OpacityMask != null) return;
            _lastMaskKey = key;

            UpdateLayout(); // polohy textů se berou z aktuálního rozvržení
            var items = new List<TextMask.Item> { MaskItem(TimeTextBlock, CurrentTypeface) };
            if (BellPanel.Visibility == Visibility.Visible)
            {
                items.Add(MaskItem(BellTextBlock, PlainTypeface(BellTextBlock)));
                if (FiveTextBlock.Visibility == Visibility.Visible) items.Add(MaskItem(FiveTextBlock, PlainTypeface(FiveTextBlock)));
            }

            OpacityMask = TextMask.Create(
                items,
                new Size(ActualWidth, ActualHeight),
                MainBorder.CornerRadius.TopLeft,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private static Typeface PlainTypeface(System.Windows.Controls.TextBlock block) =>
            new(block.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        private TextMask.Item MaskItem(System.Windows.Controls.TextBlock block, Typeface typeface) =>
            new(block.Text, typeface, block.FontSize, block.TranslatePoint(new Point(0, 0), this));

        // ---------------------------------------------------------------------------------------
        // Přichycení k okrajům a přetahování
        // ---------------------------------------------------------------------------------------

        private void SnapToEdges()
        {
            var result = ScreenSnapper.Snap(
                new Rect(Left, Top, ActualWidth, ActualHeight),
                ScreenHelper.GetWorkArea(this),
                SnapDistance);

            _snap = result.State;

            _snapping = true;
            Left = result.Position.X;
            Top = result.Position.Y;
            _snapping = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Až po načtení okna je známá jeho skutečná velikost
            SnapToEdges();
        }

        private void Window_LocationChanged(object? sender, EventArgs e)
        {
            if (!_dragging && !_snapping) SnapToEdges();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragging = true;
            _dragStart = e.GetPosition(this);
            CaptureMouse();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_dragging || e.LeftButton != MouseButtonState.Pressed) return;

            Vector offset = e.GetPosition(this) - _dragStart;
            Left += offset.X;
            Top += offset.Y;
            SnapToEdges();
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging) return;

            _dragging = false;
            ReleaseMouseCapture();
            SnapToEdges();
            SaveSettings();
        }

        // ---------------------------------------------------------------------------------------
        // Kolečko myši
        // ---------------------------------------------------------------------------------------

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            bool up = e.Delta > 0;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;

            if (shift && ctrl) _settings.CornerRadius += up ? CornerRadiusStep : -CornerRadiusStep;
            else if (shift) ChangeTextColor(up);
            else if (ctrl) ChangeBackgroundColor(up);
            else if (alt) _settings.Opacity += up ? OpacityStep : -OpacityStep;
            else _settings.FontSize += up ? FontSizeStep : -FontSizeStep;

            _settings.Normalize();
            ApplySettings();
            SnapToEdges();
            e.Handled = true;
        }

        // Změna barvy kolečkem zruší příslušný průhledný režim - nová barva je vždy plná
        private void ChangeTextColor(bool up)
        {
            _settings.TextColor = AdjustColor(_settings.TextColor, up);
            _settings.TransparentText = false;
        }

        private void ChangeBackgroundColor(bool up)
        {
            _settings.BackgroundColor = AdjustColor(_settings.BackgroundColor, up);
            _settings.TransparentBackground = false;
        }

        private static Color AdjustColor(Color color, bool up)
        {
            double change = up ? ColorStep : -ColorStep;
            return Color.FromRgb(Shift(color.R, change), Shift(color.G, change), Shift(color.B, change));

            static byte Shift(byte value, double change) => (byte)Math.Clamp(value + change, 0, 255);
        }

        // ---------------------------------------------------------------------------------------
        // Menu
        // ---------------------------------------------------------------------------------------

        private void Option_Changed(object sender, RoutedEventArgs e)
        {
            if (_updatingMenu) return;

            // Průhledné pozadí a průhledný text se vylučují - zapnutím jednoho se druhé vypne
            if (sender == TransparentBackgroundMenuItem && TransparentBackgroundMenuItem.IsChecked)
            {
                SetChecked(TransparentTextMenuItem, false);
            }
            else if (sender == TransparentTextMenuItem && TransparentTextMenuItem.IsChecked)
            {
                SetChecked(TransparentBackgroundMenuItem, false);
            }

            ReadMenuIntoSettings();
            ApplyAppearance();
            UpdateMenuVisibility();
            UpdateClock();
        }

        private void SetChecked(MenuItem item, bool value)
        {
            _updatingMenu = true;
            item.IsChecked = value;
            _updatingMenu = false;
        }

        private void DefaultView_Click(object sender, RoutedEventArgs e)
        {
            _settings = new AppSettings();
            RestorePosition();
            ApplySettings();
            SnapToEdges();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            var help = new HelpWindow();
            if (IsVisible) help.Owner = this;
            help.ShowDialog();
        }

        // ---------------------------------------------------------------------------------------
        // Zobrazení, skrytí a ukončení
        // ---------------------------------------------------------------------------------------

        private void ToggleWindowVisibility_Click(object sender, RoutedEventArgs e) => ToggleWindowVisibility();

        private void ToggleWindowVisibility()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _exiting = true;
            SaveSettings(showError: true);
            Application.Current.Shutdown();
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Při ukončení aplikace nebo Windows se okno zavře normálně, jinak by bránilo odhlášení
            if (_exiting) return;

            // Jinak okno jen skryjeme (aplikace běží dál v oznamovací oblasti)
            e.Cancel = true;
            Hide();
            SaveSettings();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            _tray.Dispose();
            _bells.Dispose();
            base.OnClosed(e);
        }
    }
}
