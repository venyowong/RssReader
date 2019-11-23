using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Coravel.Invocable;
using Dapper;
using Microsoft.Extensions.Logging;
using Rss.Common.Entities;
using RssServer.Helpers;

namespace RssServer
{
    public class RssFetcher : IInvocable
    {
        private Helper helper;

        private ILogger<RssFetcher> logger;

        public RssFetcher(Helper helper, ILogger<RssFetcher> logger)
        {
            this.helper = helper;
            this.logger = logger;
        }

        public (string Title, List<Article> Articles) Fetch(string feed)
        {
            try
            {
                var feedId = feed.Md5();
                SyndicationFeed sf = null;
                string title = null;
                CodeHollow.FeedReader.Feed cfeed = null;
                try 
                {
                    sf = SyndicationFeed.Load(XmlReader.Create(feed));
                    title = sf?.Title?.Text;
                }
                catch
                {
                    cfeed = CodeHollow.FeedReader.FeedReader.ReadAsync(feed).Result;
                    title = cfeed?.Title;
                }
                if (string.IsNullOrWhiteSpace(title))
                {
                    this.logger.LogWarning($"The title of feed({feed}) is null or white space.");
                    return (null, null);
                }

                using (var connection = this.helper.GetDbConnection())
                {
                    if (sf != null)
                    {
                        return (title, this.helper.ParseArticles(sf, feedId, connection));
                    }
                    else
                    {
                        return (title, this.helper.ParseArticles(cfeed, feedId, connection));
                    }
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e, $"failed to add {feed}");
            }

            return (null, null);
        }

        public async Task Invoke()
        {
            using (var connection = this.helper.GetDbConnection())
            {
                var feeds = await connection.QueryAsync<Feed>("SELECT * FROM feed");
                if (feeds != null && feeds.Any())
                {
                    foreach(var feed in feeds)
                    {
                        this.Fetch(feed.Url);
                    }
                }
            }
        }
    }
}