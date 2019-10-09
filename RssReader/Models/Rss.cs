using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssReader.Models
{
    public class RssDbContext : DbContext
    {
        public DbSet<ReadRecord> ReadRecords { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=rss.db");
    }

    public class ReadRecord
    {
        [Key]
        public string Url { get; set; }

        public DateTime Time { get; set; }
    }
}
