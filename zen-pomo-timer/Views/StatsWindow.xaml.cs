using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScottPlot;
using ScottPlot.TickGenerators;
using zen_pomo_timer.Models;

namespace zen_pomo_timer.Views
{
    public partial class StatsWindow : Window
    {
        private readonly StatsService _service = new();

        public StatsWindow()
        {
            InitializeComponent(); // now used for monthly chart
            LoadData();
        }

        private void LoadData()
        {
            // Streaks
            var streaks = _service.GetStreakStatistics();
            txtCurrentStreak.Text = $"Current Streak: {streaks.CurrentStreak} days";
            txtBestStreak.Text = $"{streaks.BestStreak} days";

            // All‑time totals
            var (sessions, hours) = _service.GetAllTimeTotals();
            txtAllTimeSessions.Text = sessions.ToString();
            txtAllTimeHours.Text = hours.ToString();

            // Charts
            RenderToday();
            RenderWeek();
            RenderMonthlySummary();
        }

        private void RenderToday()
        {
            var hourlyData = _service.GetHourlyStats(DateTime.Today);
            double[] values = new double[24];
            for (int i = 0; i < 24; i++)
                values[i] = hourlyData.ContainsKey(i) ? hourlyData[i] : 0;

            lblTodayTotal.Text = $"Total: {Math.Round(values.Sum() / 60.0, 1)} hours";

            chartToday.Plot.Clear();
            chartToday.Plot.Add.Bars(values);
            chartToday.Plot.Axes.Bottom.TickGenerator = new NumericAutomatic();
            chartToday.Plot.Title("Minutes Focused by Hour");
            chartToday.Refresh();
            chartToday.UserInputProcessor.Disable();
        }

        private void RenderWeek()
        {
            var today = DateTime.Today;
            var dailyStats = _service.GetWeeklyDailyStats(today);

            // Extract values and labels (ordered from oldest to newest)
            var dates = dailyStats.Keys.OrderBy(d => d).ToList();
            double[] values = dates.Select(d => (double)dailyStats[d]).ToArray();
            double[] positions = dates.Select((_, i) => (double)i).ToArray();

            // Short day names (Mon, Tue ...)
            string[] labels = dates.Select(d => d.ToString("ddd")).ToArray();

            chartWeek.Plot.Clear();
            var bars = chartWeek.Plot.Add.Bars(positions, values);
            chartWeek.Plot.Axes.Bottom.SetTicks(positions, labels);
            chartWeek.Plot.Axes.Bottom.TickLabelStyle.Rotation = 0; // horizontal
            chartWeek.Plot.Title("Minutes Focused by Day (Last 7 Days)");
            chartWeek.Refresh();
            chartWeek.UserInputProcessor.Disable();
        }

        private void RenderMonthlySummary()
        {
            int year = DateTime.Today.Year;
            var monthlyStats = _service.GetMonthlyStats(year);

            double[] values = new double[12];
            for (int m = 1; m <= 12; m++)
                values[m - 1] = monthlyStats[m] / 60.0; // hours

            string[] monthLabels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                     "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            double[] positions = Enumerable.Range(0, 12).Select(i => (double)i).ToArray();

            chartHeatMap.Plot.Clear();
            var bars = chartHeatMap.Plot.Add.Bars(positions, values);

            chartHeatMap.Plot.Axes.Bottom.SetTicks(positions, monthLabels);
            chartHeatMap.Plot.Axes.Bottom.TickLabelStyle.Rotation = 0;
            // tilted for readability
            chartHeatMap.Plot.Title("Monthly Focus Hours");
            chartHeatMap.Plot.YLabel("Hours");
            chartHeatMap.Refresh();
            chartHeatMap.UserInputProcessor.Disable();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
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
        }
    }
}