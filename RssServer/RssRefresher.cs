using Microsoft.Extensions.Options;
using Niolog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace RssServer
{
    public class RssRefresher
    {
        private ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

        private int worker = 3;

        private AppSettings appSettings;

        public RssRefresher(IOptions<AppSettings> options)
        {
            this.appSettings = options?.Value;

            for(int i = 0; i < this.worker; i++)
            {
                Task.Run(this.Consumer);
            }
        }

        private void Consumer()
        {
            while (true)
            {
                if (this.queue.Count <= 0)
                {
                    SpinWait.SpinUntil(() => this.queue.Count > 0);
                }

                if (!this.queue.TryDequeue(out string feed))
                {
                    continue;
                }

                try
                {
                    var sf = SyndicationFeed.Load(XmlReader.Create(feed));
                    using (var connection = Helper.GetDbConnection(this.appSettings?.MySql?.ConnectionString))
                    {
                        Helper.ParseArticles(sf, feed.Md5(), connection);
                    }
                    NiologManager.CreateLogger().Info()
                        .Message($"refreshed {feed}")
                        .Write();
                }
                catch (Exception e)
                {
                    NiologManager.CreateLogger().Error()
                        .Message($"error occured when refreshing {feed}")
                        .Exception(e)
                        .Write();
                }
            }
        }

        public void PushFeed(string feed)
        {
            if (string.IsNullOrWhiteSpace(feed))
            {
                return;
            }

            this.queue.Enqueue(feed);
        }
    }
}
