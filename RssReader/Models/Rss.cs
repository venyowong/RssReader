using System;

namespace RssReader.Models
{
    public class ReadRecord
    {
        public int Id { get; set; }

        public string Url { get; set; }

        public DateTime Time { get; set; }
    }
}
