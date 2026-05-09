using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zen_pomo_timer
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private readonly string _statePath;

        public SettingsService()
        {
            // Store settings in AppData folder
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ZenPomoTimer"
            );

            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            _settingsPath = Path.Combine(appDataPath, "settings.json");
            _statePath = Path.Combine(appDataPath, "state.json");
        }

        public void SaveSettings(TimerSettings settings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new TimeSpanJsonConverter() }
            };

            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsPath, json);
        }

        public TimerSettings LoadSettings()
        {
            if (!File.Exists(_settingsPath))
                return GetDefaultSettings();

            try
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new TimeSpanJsonConverter() }
                };

                string json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<TimerSettings>(json, options) ?? GetDefaultSettings();
            }
            catch
            {
                return GetDefaultSettings();
            }
        }

        public void SaveState(AppState state)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(state, options);
            File.WriteAllText(_statePath, json);
        }

        public AppState LoadState()
        {
            if (!File.Exists(_statePath))
                return new AppState();

            try
            {
                string json = File.ReadAllText(_statePath);
                return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
            }
            catch
            {
                return new AppState();
            }
        }

        private TimerSettings GetDefaultSettings()
        {
            return new TimerSettings
            {
                SessionTime = TimeSpan.FromMinutes(25),
                BreakTime = TimeSpan.FromMinutes(5),
                LongBreakTime = TimeSpan.FromMinutes(15),
                PomodorosBeforeLongBreak = 4,
                AutoStartBreaks = false,
                AutoStartPomodoros = false,
                EnableSound = true,
                PrimaryColor = "#A7C080",
                BackgroundTheme = "Dark"
            };
        }
    }

    // Custom converter for TimeSpan serialization
    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.FromMinutes(reader.GetDouble());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.TotalMinutes);
        }
    }

    public class AppState
    {
        public int CurrentSessionCount { get; set; } = 1;
        public TimerMode CurrentMode { get; set; } = TimerMode.Pomodoro;
        public TimeSpan CurrentTimeRemaining { get; set; }
        public bool IsRunning { get; set; }
        public DateTime? LastSavedTime { get; set; }
    }
}