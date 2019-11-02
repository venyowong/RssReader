using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssReader.Models
{
    public class ArticleListPageParams
    {
        public string FeedId { get; set; }

        public DateTime EndTime { get; set; }

        public string ArticleId { get; set; }
    }
}
