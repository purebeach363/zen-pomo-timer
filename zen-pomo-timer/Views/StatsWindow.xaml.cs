using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using zen_pomo_timer.Models.Data;

namespace zen_pomo_timer.Views
{
    public partial class StatsWindow : Window
    {
        public StatsWindow()
        {
            InitializeComponent();
            LoadStats();
        }

        private void LoadStats()
        {
            try
            {
                using var db = new AppDbContext();

                // Get all sessions
                var allSessions = db.Stats.OrderByDescending(s => s.TimeStamp).ToList();

                // Total stats
                int totalPomodoros = allSessions.Count;
                int totalMinutes = allSessions.Sum(s => s.DurationMinutes);
                int todayMinutes = allSessions
                    .Where(s => s.TimeStamp.Date == DateTime.Today)
                    .Sum(s => s.DurationMinutes);

                // Update summary cards
                txtTotalPomodoros.Text = totalPomodoros.ToString();
                txtTotalMinutes.Text = totalMinutes.ToString();
                txtTodayMinutes.Text = todayMinutes.ToString();

                // Last 7 days stats
                var last7Days = new List<WeeklyStat>();
                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);
                    var daySessions = allSessions.Where(s => s.TimeStamp.Date == date).ToList();
                    var dayMinutes = daySessions.Sum(s => s.DurationMinutes);

                    last7Days.Add(new WeeklyStat
                    {
                        Date = date.ToString("ddd, MMM dd"),
                        Pomodoros = daySessions.Count,
                        Minutes = dayMinutes
                    });
                }
                lstWeeklyStats.ItemsSource = last7Days;

                // All sessions history
                var sessionList = allSessions.Select(s => new SessionHistory
                {
                    Date = s.TimeStamp.ToString("yyyy-MM-dd HH:mm"),
                    Duration = $"{s.DurationMinutes} min",
                    PomodoroCount = 1
                }).ToList();

                lstAllSessions.ItemsSource = sessionList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading stats: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class WeeklyStat
    {
        public string Date { get; set; }
        public int Pomodoros { get; set; }
        public int Minutes { get; set; }
    }

    public class SessionHistory
    {
        public string Date { get; set; }
        public string Duration { get; set; }
        public int PomodoroCount { get; set; }
    }
}