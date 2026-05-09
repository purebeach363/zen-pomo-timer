using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using zen_pomo_timer.Views;
using zen_pomo_timer.Models.Data;
using zen_pomo_timer.Models;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;

namespace zen_pomo_timer
{
    public partial class MainWindow : Window
    {
        private Notifier _notifier;
        private void InitNotifier()
        {
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: ToastNotifications.Position.Corner.BottomRight,  // Change to BottomLeft
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(3));

                cfg.Dispatcher = Application.Current.Dispatcher;

                // Extra: Make top-most and set width
                cfg.DisplayOptions.TopMost = true;
                cfg.DisplayOptions.Width = 350;
            });
        }
        #region declaration

        public TimerSettings settings;
        private SettingsService _settingsService;

        private TimeSpan currentTime;
        private int count = 1;
        private bool isRunning = false;
        private DateTime? _pauseTime;

        private DispatcherTimer timer;
        private TimerMode timerMode;
        private int _autoSaveCounter = 0;

        private bool _isLocked = false;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            InitNotifier();

            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
            }

            _settingsService = new SettingsService();

            // Load settings
            settings = _settingsService.LoadSettings();

            // Load application state
            LoadAppState();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            UpdateDisplay();
            ApplyTheme();

            // Save settings when window closes
            this.Closed += MainWindow_Closed;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isLocked && e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void LoadAppState()
        {
            var state = _settingsService.LoadState();

            count = state.CurrentSessionCount;
            timerMode = state.CurrentMode;

            // Calculate elapsed time since last save if timer was running
            if (state.IsRunning && state.LastSavedTime.HasValue)
            {
                var elapsed = DateTime.Now - state.LastSavedTime.Value;
                currentTime = state.CurrentTimeRemaining - TimeSpan.FromSeconds(elapsed.TotalSeconds);

                if (currentTime.TotalSeconds <= 0)
                {
                    // Timer would have expired, handle appropriately
                    currentTime = TimeSpan.Zero;
                    // Use Dispatcher to handle the expired event after window loads
                    Dispatcher.BeginInvoke(new Action(() => HandleTimerExpired()));
                }
                else
                {
                    // Don't start timer here - wait until window is fully loaded
                    isRunning = true;
                    btnStartStop.Content = "Stop";
                    // Start timer after window is loaded
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (isRunning && timer != null)
                        {
                            timer.Start();
                        }
                    }));
                }
            }
            else
            {
                // Set current time based on mode
                SetCurrentTimeByMode();
            }
        }

        private void SetCurrentTimeByMode()
        {
            switch (timerMode)
            {
                case TimerMode.Pomodoro:
                    currentTime = settings.SessionTime;
                    break;
                case TimerMode.ShortBreak:
                    currentTime = settings.BreakTime;
                    break;
                case TimerMode.LongBreak:
                    currentTime = settings.LongBreakTime;
                    break;
            }
        }

        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                timer.Start();
                btnStartStop.Content = "Stop";
                _pauseTime = null;
            }
            else
            {
                timer.Stop();
                btnStartStop.Content = "Start";
                _pauseTime = DateTime.Now;
            }

            isRunning = !isRunning;
            SaveAppState(); // Save state on pause/start
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (currentTime.TotalSeconds > 0)
            {
                currentTime -= TimeSpan.FromSeconds(1);
                UpdateDisplay();

                _autoSaveCounter++;
                if (_autoSaveCounter >= 5)
                {
                    _autoSaveCounter = 0;
                    SaveAppState();
                }
            }
            else
            {
                timer.Stop();
                isRunning = false;
                btnStartStop.Content = "Start";

                if (settings.EnableSound)
                    PlaySound();

                HandleTimerExpired();
            }
        }

        private void HandleTimerExpired()
        {
            string content = "Time is up!";
            _notifier.ShowInformation(content);
            if (timerMode == TimerMode.Pomodoro)
            {
                SavePomodoroSession();
            }

            SwitchMode();

            // Auto-start based on settings
            if ((timerMode == TimerMode.Pomodoro && settings.AutoStartPomodoros) ||
                (timerMode != TimerMode.Pomodoro && settings.AutoStartBreaks))
            {
                timer.Start();
                isRunning = true;
                btnStartStop.Content = "Stop";
            }
            else
            {
                isRunning = false;
                timer.Stop();
                btnStartStop.Content = "Start";
            }
                SaveAppState();
        }

        private void SwitchMode()
        {
            if (timerMode == TimerMode.Pomodoro)
            {
                if (count % settings.PomodorosBeforeLongBreak == 0)
                {
                    timerMode = TimerMode.LongBreak;
                    currentTime = settings.LongBreakTime;
                }
                else
                {
                    timerMode = TimerMode.ShortBreak;
                    currentTime = settings.BreakTime;
                }
            }
            else
            {
                timerMode = TimerMode.Pomodoro;
                currentTime = settings.SessionTime;
                count++;
            }

            UpdateDisplay();
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            isRunning = false;
            btnStartStop.Content = "Start";
            SwitchMode();
            SaveAppState();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            isRunning = false;

            timerMode = TimerMode.Pomodoro;
            count = 1;
            currentTime = settings.SessionTime;

            btnStartStop.Content = "Start";
            UpdateDisplay();
            SaveAppState();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            // Pause timer while settings are open
            bool wasRunning = isRunning;
            if (wasRunning)
            {
                timer.Stop();
                isRunning = false;
                btnStartStop.Content = "Start";
            }

            SettingsWindow window = new SettingsWindow(settings);
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                settings = window.currentSettings;
                _settingsService.SaveSettings(settings);

                // Update current time based on new settings
                SetCurrentTimeByMode();
                ApplyTheme();
                UpdateDisplay();
            }

            // Resume if it was running
            if (wasRunning)
            {
                timer.Start();
                isRunning = true;
                btnStartStop.Content = "Stop";
            }
        }

        private void ApplyTheme()
        {
            var converter = new BrushConverter();
            // Retrieve the color from settings
            var primaryBrush = (Brush)converter.ConvertFromString(settings.PrimaryColor);

            // Apply primary color only to elements that need it
            btnStartStop.Background = primaryBrush;
            btnSkip.BorderBrush = primaryBrush; // Outlined buttons use BorderBrush for the ring
            btnSkip.Foreground = primaryBrush;
            btnSettings.Foreground = primaryBrush;
            btnRefresh.Foreground = primaryBrush;
            btnStats.Foreground = primaryBrush;
            SyncGlobalResourcesToSettings();
        }

        private void SyncGlobalResourcesToSettings()
        {
            var conv = ColorConverter.ConvertFromString;
            // This is the color the user chose (e.g., Sage #A7C080)
            var primaryColor = (Color)conv(settings.PrimaryColor);
            var primaryBrush = new SolidColorBrush(primaryColor);

            // 1. Update your custom key used in XAML
            Application.Current.Resources["PrimaryColor"] = primaryBrush;

            // 2. Overwrite Material Design's internal "Purple" keys
            // This effectively "kills" the purple everywhere in the app
            Application.Current.Resources["PrimaryHueMidBrush"] = primaryBrush;
            Application.Current.Resources["PrimaryHueLightBrush"] = primaryBrush;
            Application.Current.Resources["PrimaryHueDarkBrush"] = primaryBrush;

            // This specifically handles the "Ripple" effect and button mouse-over
            Application.Current.Resources["SecondaryAccentBrush"] = primaryBrush;
            Application.Current.Resources["MaterialDesignSelection"] = primaryBrush;

            // 3. Handle Dark/Light Mode Backgrounds
            if (settings.BackgroundTheme == "Dark")
            {
                // ZEN DARK - High Contrast
                var darkBg = new SolidColorBrush((Color)conv("#1E2326")); // Keep the deep charcoal
                var whiteText = new SolidColorBrush(Colors.White); // Pure White

                Application.Current.Resources["ForeColor"] = whiteText;
                Application.Current.Resources["MaterialDesignPaper"] = darkBg;
                Application.Current.Resources["MaterialDesignBody"] = whiteText;
            }
            else
            {
                var lightBg = new SolidColorBrush((Color)conv("#FCFDFF"));
                var textBrush = new SolidColorBrush((Color)conv("#2D3436"));

                Application.Current.Resources["ForeColor"] = textBrush;
                Application.Current.Resources["MaterialDesignPaper"] = lightBg;
                Application.Current.Resources["MaterialDesignBody"] = textBrush;

                // This adds a very subtle divider line color for light mode
                Application.Current.Resources["MaterialDesignDivider"] = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
            }
        }

        private void UpdateDisplay()
        {
            TextBlockTime.Text = currentTime.ToString(@"mm\:ss");
            TextBlockCount.Text = $"Session: {count}";
        }

        public void PlaySound()
        {
            try
            {
                // Get the base directory (where the .exe is running from)
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Check if the sound file exists in multiple possible locations
                string soundPath = System.IO.Path.Combine(baseDirectory, "Assets", "Sounds", settings.NotificationSound);

                // Also check if it might be in a different location (for development)
                if (!File.Exists(soundPath))
                {
                    // Try looking in the project directory (for development)
                    string projectPath = System.IO.Path.Combine(
                        Directory.GetParent(baseDirectory).Parent.Parent.FullName,
                        "Assets", "Sounds", settings.NotificationSound);

                    if (File.Exists(projectPath))
                        soundPath = projectPath;
                }

                if (File.Exists(soundPath))
                {
                    SoundPlayer player = new SoundPlayer(soundPath);
                    player.Play(); // Use PlaySync() if you want to block until sound finishes
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Sound file not found: {soundPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
            }
        }

        private void SaveAppState()
        {
            var state = new AppState
            {
                CurrentSessionCount = count,
                CurrentMode = timerMode,
                CurrentTimeRemaining = currentTime,
                IsRunning = isRunning,
                LastSavedTime = isRunning ? DateTime.Now : (DateTime?)null
            };

            _settingsService.SaveState(state);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            SaveAppState();
        }

        private void btnStats_Click(object sender, RoutedEventArgs e)
        {
            bool wasRunning = isRunning;
            if (wasRunning)
            {
                timer.Stop();
                isRunning = false;
                btnStartStop.Content = "Start";
            }

            StatsWindow statsWindow = new StatsWindow();
            statsWindow.Owner = this;
            statsWindow.ShowDialog();

            if (wasRunning)
            {
                timer.Start();
                isRunning = true;
                btnStartStop.Content = "Stop";
            }
        }

        private void SavePomodoroSession()
        {
            try
            {
                using (var db = new Models.Data.AppDbContext())
                {
                    db.Stats.Add(new Stats
                    {
                        TimeStamp = DateTime.Now,
                        DurationMinutes = (int)settings.SessionTime.TotalMinutes
                    });

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB Error: {ex.Message}");
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    // Restore to normal size
                    this.WindowState = WindowState.Normal;
                }
                else
                {
                    // Enter fullscreen/maximized mode
                    // Note: WindowStyle.None removes the title bar for a "true" fullscreen feel
                    this.WindowState = WindowState.Maximized;
                }
            }
            if (e.Key == Key.L && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
            {

                if (this.Left == 0 && this.Top == 0 && this.Topmost)
                {
                    this.Topmost = false;
                    _isLocked = false;  

                    double screenWidth = SystemParameters.PrimaryScreenWidth;
                    double screenHeight = SystemParameters.PrimaryScreenHeight;

                    this.Left = (screenWidth / 2) - (this.Width / 2);
                    this.Top = (screenHeight / 2) - (this.Height / 2);
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    this.Left = 0;
                    this.Top = 0;
                    this.Topmost = true;
                    _isLocked = true;
                }
            }
        }
        private void UpdateResource(string key, object value)
        {
            Application.Current.Resources[key] = value;
        }
    }

    public enum TimerMode
    {
        Pomodoro,
        ShortBreak,
        LongBreak
    }
}