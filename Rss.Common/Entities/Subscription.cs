using System;
using System.Collections.Generic;
using System.Text;

namespace Rss.Common.Entities
{
    public class Subscription
    {
        public int Id { get; set; }

        public string AppId { get; set; }

        public string FeedId { get; set; }
    }
}
