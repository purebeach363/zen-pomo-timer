using System;
using System.Collections.Generic;
using System.Linq;
using zen_pomo_timer.Models.Data;

namespace zen_pomo_timer.Models
{
    public class StreakResult
    {
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
    }

    public class StatsService
    {
        public StreakResult GetStreakStatistics()
        {
            using var db = new AppDbContext();
            var dates = db.Stats
                .Select(s => s.TimeStamp.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!dates.Any())
                return new StreakResult { CurrentStreak = 0, BestStreak = 0 };

            int bestStreak = 0;
            int tempStreak = 0;
            DateTime? lastDate = null;

            foreach (var date in dates)
            {
                if (lastDate == null || date == lastDate.Value.AddDays(1))
                    tempStreak++;
                else
                    tempStreak = 1;

                if (tempStreak > bestStreak)
                    bestStreak = tempStreak;

                lastDate = date;
            }

            // Current streak
            int currentRunningStreak = 0;
            var today = DateTime.Today;
            var lastSessionDate = dates.Last();

            if (lastSessionDate == today || lastSessionDate == today.AddDays(-1))
            {
                DateTime checkDate = lastSessionDate;
                for (int i = dates.Count - 1; i >= 0; i--)
                {
                    if (dates[i] == checkDate)
                    {
                        currentRunningStreak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                    else break;
                }
            }

            return new StreakResult
            {
                CurrentStreak = currentRunningStreak,
                BestStreak = bestStreak
            };
        }

        public Dictionary<int, int> GetHourlyStats(DateTime date)
        {
            using var db = new AppDbContext();
            return db.Stats
                .Where(s => s.TimeStamp.Date == date.Date)
                .AsEnumerable()
                .GroupBy(s => s.TimeStamp.Hour)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes));
        }

        /// <summary>Returns minutes of focus for each of the last 7 days (key = DateTime.Date).</summary>
        public Dictionary<DateTime, int> GetWeeklyDailyStats(DateTime today)
        {
            using var db = new AppDbContext();
            var start = today.AddDays(-6).Date;
            var end = today.Date.AddDays(1); // exclusive

            var stats = db.Stats
                .Where(s => s.TimeStamp >= start && s.TimeStamp < end)
                .AsEnumerable()
                .GroupBy(s => s.TimeStamp.Date)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes));

            // Ensure all 7 days exist with 0 if no sessions
            var result = new Dictionary<DateTime, int>();
            for (int i = 0; i < 7; i++)
            {
                var day = start.AddDays(i);
                result[day] = stats.ContainsKey(day) ? stats[day] : 0;
            }
            return result;
        }

        /// <summary>Returns total minutes per month for the given year.</summary>
        public Dictionary<int, int> GetMonthlyStats(int year)
        {
            using var db = new AppDbContext();
            var stats = db.Stats
                .Where(s => s.TimeStamp.Year == year)
                .AsEnumerable()
                .GroupBy(s => s.TimeStamp.Month)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes));

            var result = new Dictionary<int, int>();
            for (int m = 1; m <= 12; m++)
                result[m] = stats.ContainsKey(m) ? stats[m] : 0;
            return result;
        }

        /// <summary>All‑time totals.</summary>
        public (int totalSessions, double totalHours) GetAllTimeTotals()
        {
            using var db = new AppDbContext();
            int sessions = db.Stats.Count();
            int totalMinutes = db.Stats.Sum(s => s.DurationMinutes);
            double hours = Math.Round(totalMinutes / 60.0, 1);
            return (sessions, hours);
        }
    }
}