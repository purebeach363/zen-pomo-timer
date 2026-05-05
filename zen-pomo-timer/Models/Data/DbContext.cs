using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using zen_pomo_timer.Models;

namespace zen_pomo_timer.Models.Data
{
    public class AppDbContext: DbContext
    {
        public DbSet<Stats> Stats { get; set; }

        // In DbContext.cs
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ZenPomoTimer");
            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            string dbPath = Path.Combine(appDataPath, "zen-pomo.db");
            options.UseSqlite($"Data Source={dbPath}");
        }
    }
}