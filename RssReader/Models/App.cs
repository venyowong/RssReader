using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssReader.Models
{
    public class App
    {
        [Key]
        public string Id { get; set; }

        public string BaseUrl { get; set; } = "https://venyo.cn/rss";
    }

    public class AppDbContext : DbContext
    {
        public DbSet<App> App { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=app.db");
    }
}
