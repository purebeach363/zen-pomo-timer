using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace zen_pomo_timer.Models.Data
{
    [Index(nameof(TimeStamp))]
    public class Stats
    {
        [Key]
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public int DurationMinutes { get; set; }
    }
}
