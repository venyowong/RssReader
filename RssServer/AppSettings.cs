using System;
using System.Collections.Generic;
using System.Text;

namespace RssServer
{
    public class AppSettings
    {
        public MySqlConfig MySql { get; set; }

        public LogConfig Log { get; set; }
    }

    public class MySqlConfig
    {
        public string ConnectionString { get; set; }
    }

    public class LogConfig
    {
        public string Path { get; set; }
    }
}
