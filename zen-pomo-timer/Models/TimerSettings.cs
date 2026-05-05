namespace zen_pomo_timer
{
    public class TimerSettings
    {
        public int PomodorosBeforeLongBreak { get; set; }
        public TimeSpan SessionTime { get; set; }
        public TimeSpan BreakTime { get; set; }
        public TimeSpan LongBreakTime { get; set; }

        // New properties
        public bool AutoStartBreaks { get; set; }
        public bool AutoStartPomodoros { get; set; }
        public bool EnableSound { get; set; } = true;
        public string PrimaryColor { get; set; } = "Purple";
        public string BackgroundTheme { get; set; } = "Dark";
        public string NotificationSound { get; set; } = "notification.wav";
    }
}