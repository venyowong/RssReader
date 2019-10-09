using Rss.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rss.Common.Models
{
    public class RssModel
    {
        public string Title { get; set; }

        public List<Article> Articles { get; set; }
    }
}
