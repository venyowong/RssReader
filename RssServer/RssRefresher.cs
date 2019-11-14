using CodeHollow.FeedReader;
using Microsoft.Extensions.Options;
using Niolog;
using RssServer.Helpers;
using System;
using System.Collections.Concurrent;
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

        private Helper helper;

        public RssRefresher(Helper helper)
        {
            this.helper = helper;

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
                    Thread.Sleep(1000);
                }

                if (!this.queue.TryDequeue(out string feed))
                {
                    continue;
                }

                try
                {
                    var sf = SyndicationFeed.Load(XmlReader.Create(feed));
                    using (var connection = this.helper.GetDbConnection())
                    {
                        this.helper.ParseArticles(sf, feed.Md5(), connection);
                    }
                    NiologManager.CreateLogger().Info()
                        .Message($"refreshed {feed}")
                        .Write();
                }
                catch
                {
                    try 
                    {
                        var feedEntity = FeedReader.ReadAsync(feed).Result;
                        using (var connection = this.helper.GetDbConnection())
                        {
                            this.helper.ParseArticles(feedEntity, feed.Md5(), connection);
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
