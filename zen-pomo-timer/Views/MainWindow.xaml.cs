using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using zen_pomo_timer.Views;
using zen_pomo_timer.Models.Data;
using zen_pomo_timer.Models;
using zen_pomo_timer.Views; 

namespace zen_pomo_timer
{
    public partial class MainWindow : Window
    {

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

        #endregion

        public MainWindow()
        {
            InitializeComponent();

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
            if(timerMode == TimerMode.Pomodoro)
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
            // Apply primary color to buttons
            var converter = new BrushConverter();
            var purpleBrush = (Brush)converter.ConvertFromString("Purple");
            var primaryBrush = (Brush)converter.ConvertFromString(settings.PrimaryColor);

            btnStartStop.Background = primaryBrush;
            btnSkip.Background = primaryBrush;
            btnSettings.Foreground = primaryBrush;
            btnRefresh.Foreground = primaryBrush;
            btnStats.Foreground = primaryBrush;

            // Apply background theme
            if (settings.BackgroundTheme == "Light")
            {
                this.Background = (Brush)converter.ConvertFromString("#F5F5F5");
                StackPanelTimer.Background = (Brush)converter.ConvertFromString("#F5F5F5");
                TextBlockTime.Foreground = (Brush)converter.ConvertFromString("#333333");
                TextBlockCount.Foreground = (Brush)converter.ConvertFromString("#333333");
            }
            else
            {
                this.Background = (Brush)converter.ConvertFromString("#333333");
                StackPanelTimer.Background = (Brush)converter.ConvertFromString("#333333");
                TextBlockTime.Foreground = Brushes.White;
                TextBlockCount.Foreground = Brushes.White;
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
    }

    public enum TimerMode
    {
        Pomodoro,
        ShortBreak,
        LongBreak
    }
}