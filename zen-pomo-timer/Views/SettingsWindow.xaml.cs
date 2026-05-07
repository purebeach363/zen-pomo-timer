using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace zen_pomo_timer
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public TimerSettings currentSettings;
        private TimerSettings _originalSettings;

        private Dictionary<string, string> _soundFileMap = new Dictionary<string, string>
        {
            { "Default", "notification.wav" },
            { "Calm", "Calm.wav" },
            { "Chord", "Chord.wav" },
            { "Chord2", "Chord2.wav" },
            { "Cloud", "Cloud.wav" },
            { "Glisten", "Glisten.wav" },
            { "Jinja", "Jinja.wav" },
            { "Jinja2", "Jinja2.wav" },
            { "Polite", "Polite.wav" }
        };

        public SettingsWindow(TimerSettings settings)
        {
            currentSettings = settings;
            _originalSettings = new TimerSettings
            {
                SessionTime = settings.SessionTime,
                BreakTime = settings.BreakTime,
                LongBreakTime = settings.LongBreakTime,
                PomodorosBeforeLongBreak = settings.PomodorosBeforeLongBreak,
                AutoStartBreaks = settings.AutoStartBreaks,
                AutoStartPomodoros = settings.AutoStartPomodoros,
                EnableSound = settings.EnableSound,
                PrimaryColor = settings.PrimaryColor,
                BackgroundTheme = settings.BackgroundTheme,
                NotificationSound = settings.NotificationSound
            };
            InitializeComponent();
            LoadSettings();
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, "^[0-9]+$"))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void txtSessionTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtSessionTime.Text, out int minutes) && minutes > 0)
            {
                currentSettings.SessionTime = TimeSpan.FromMinutes(minutes);
            }
            else if (!string.IsNullOrEmpty(txtSessionTime.Text))
            {
                currentSettings.SessionTime = TimeSpan.FromMinutes(25);
            }
        }

        private void txtShortBreak_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtShortBreak.Text, out int minutes) && minutes > 0)
            {
                currentSettings.BreakTime = TimeSpan.FromMinutes(minutes);
            }
            else if (!string.IsNullOrEmpty(txtShortBreak.Text))
            {
                currentSettings.BreakTime = TimeSpan.FromMinutes(5);
            }
        }

        private void txtLongBreak_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtLongBreak.Text, out int minutes) && minutes > 0)
            {
                currentSettings.LongBreakTime = TimeSpan.FromMinutes(minutes);
            }
            else if (!string.IsNullOrEmpty(txtLongBreak.Text))
            {
                currentSettings.LongBreakTime = TimeSpan.FromMinutes(15);
            }
        }

        private void txtPomodoroBeforeLongBreak_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtPomodoroBeforeLongBreak.Text, out int pomodoros) && pomodoros > 0)
            {
                currentSettings.PomodorosBeforeLongBreak = pomodoros;
            }
            else if (!string.IsNullOrEmpty(txtPomodoroBeforeLongBreak.Text))
            {
                currentSettings.PomodorosBeforeLongBreak = 4;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Restore original settings
            currentSettings.SessionTime = _originalSettings.SessionTime;
            currentSettings.BreakTime = _originalSettings.BreakTime;
            currentSettings.LongBreakTime = _originalSettings.LongBreakTime;
            currentSettings.PomodorosBeforeLongBreak = _originalSettings.PomodorosBeforeLongBreak;
            currentSettings.AutoStartBreaks = _originalSettings.AutoStartBreaks;
            currentSettings.AutoStartPomodoros = _originalSettings.AutoStartPomodoros;
            currentSettings.EnableSound = _originalSettings.EnableSound;
            currentSettings.PrimaryColor = _originalSettings.PrimaryColor;
            currentSettings.BackgroundTheme = _originalSettings.BackgroundTheme;
            currentSettings.NotificationSound = _originalSettings.NotificationSound;

            this.DialogResult = false;
            this.Close();
        }

        private void LoadSettings()
        {
            var converter = new BrushConverter();
            var brush = (SolidColorBrush)converter.ConvertFromString(currentSettings.PrimaryColor);

            Application.Current.Resources["PrimaryColor"] = brush;


            txtSessionTime.Text = ((int)currentSettings.SessionTime.TotalMinutes).ToString();
            txtShortBreak.Text = ((int)currentSettings.BreakTime.TotalMinutes).ToString();
            txtLongBreak.Text = ((int)currentSettings.LongBreakTime.TotalMinutes).ToString();
            txtPomodoroBeforeLongBreak.Text = currentSettings.PomodorosBeforeLongBreak.ToString();

            // Load new settings
            chkAutoStartBreaks.IsChecked = currentSettings.AutoStartBreaks;
            chkAutoStartPomodoros.IsChecked = currentSettings.AutoStartPomodoros;
            chkEnableSound.IsChecked = currentSettings.EnableSound;

            // Set the sound selection
            // Find display name based on the saved file name
            string displayName = "Default";
            foreach (var mapping in _soundFileMap)
            {
                if (mapping.Value == currentSettings.NotificationSound)
                {
                    displayName = mapping.Key;
                    break;
                }
            }

            // Select the correct item in the combo box
            foreach (ComboBoxItem item in cmbNotificationSound.Items)
            {
                if (item.Content.ToString() == displayName)
                {
                    cmbNotificationSound.SelectedItem = item;
                    break;
                }
            }

            // Set combo boxes for appearance
            foreach (ComboBoxItem item in cmbPrimaryColor.Items)
            {
                if (item.Content.ToString() == currentSettings.PrimaryColor)
                {
                    cmbPrimaryColor.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in cmbBackgroundColor.Items)
            {
                if (item.Content.ToString() == currentSettings.BackgroundTheme)
                {
                    cmbBackgroundColor.SelectedItem = item;
                    break;
                }
            }
        }

        private void DropNotificationSound_Closed(object sender, EventArgs e)
        {
            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets", "Sounds",
                _soundFileMap[cmbNotificationSound.Text]
            );

            SoundPlayer player = new SoundPlayer(path);
            player.Play();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            currentSettings.AutoStartBreaks = chkAutoStartBreaks.IsChecked ?? false;
            currentSettings.AutoStartPomodoros = chkAutoStartPomodoros.IsChecked ?? false;
            currentSettings.EnableSound = chkEnableSound.IsChecked ?? true;

            // Save sound selection - FIX: Don't add .wav twice
            if (cmbNotificationSound.SelectedItem is ComboBoxItem soundItem)
            {
                string displayName = soundItem.Content.ToString();
                // Map display name to actual filename
                if (_soundFileMap.ContainsKey(displayName))
                {
                    currentSettings.NotificationSound = _soundFileMap[displayName]; // Already includes .wav
                }
                else
                {
                    currentSettings.NotificationSound = "notification.wav"; // Default
                }
            }

            if (cmbPrimaryColor.SelectedItem is ComboBoxItem primaryItem)
                currentSettings.PrimaryColor = primaryItem.Content.ToString();

            if (cmbBackgroundColor.SelectedItem is ComboBoxItem bgItem)
                currentSettings.BackgroundTheme = bgItem.Content.ToString();

            this.DialogResult = true;
            this.Close();
        }

        private void cmbNotificationSound_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void cmbPrimaryColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPrimaryColor.SelectedItem is ComboBoxItem item)
            {
                string colorName = item.Content.ToString();

                var brush = (SolidColorBrush)new BrushConverter()
                    .ConvertFromString(colorName);

                this.Resources["PrimaryColor"] = brush; // 🔥 use THIS, not Application
            }
        }

        private void cmbBackgroundColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBackgroundColor.SelectedItem is ComboBoxItem item)
            {
                string selected = item.Content.ToString();
                var conv = ColorConverter.ConvertFromString;

                if (selected == "Dark")
                {
                    var darkBg = new SolidColorBrush((Color)conv("#333333"));
                    UpdateGlobalResource("ForeColor", Brushes.White);
                    UpdateGlobalResource("MaterialDesignPaper", darkBg); // This fixes the Main Window Border
                }
                else if (selected == "Light")
                {
                    var lightBg = new SolidColorBrush((Color)conv("#F5F5F5"));
                    UpdateGlobalResource("ForeColor", new SolidColorBrush((Color)conv("#333333")));
                    UpdateGlobalResource("MaterialDesignPaper", lightBg); // This fixes the Main Window Border
                }
            }
        }
        private void UpdateGlobalResource(string key, object value)
        {
            Application.Current.Resources[key] = value;
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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
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
        }
    }
}