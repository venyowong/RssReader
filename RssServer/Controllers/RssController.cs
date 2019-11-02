using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Xml;
using Dapper;
using System.Linq;
using Rss.Common.Models;
using Rss.Common.Entities;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using RssServer.Helpers;
using RssServer.Daos;

namespace RssServer.Controllers
{
    [Route("rss")]
    public class RssController : Controller
    {
        private ILogger<RssController> logger;

        private RssRefresher refresher;

        private Helper helper;

        public RssController(Helper helper, ILogger<RssController> logger, RssRefresher refresher)
        {
            this.helper = helper;
            this.logger = logger;
            this.refresher = refresher;
        }

        [HttpPost, Route("add"), ModelValidation]
        public object Add([Required]string feed, [Required][FromHeader] string appid)
        {
            var subscribeResult = this.SubscribeFeed(feed, appid);
            return new RssModel
            {
                Title = subscribeResult.Title,
                Articles = subscribeResult.Articles
            };
        }

        [HttpGet, Route("feeds"), ModelValidation]
        public object GetFeeds([Required][FromHeader] string appid)
        {
            using (var connection = this.helper.GetDbConnection())
            {
                return connection.Query<Feed>("SELECT * FROM feed WHERE id IN (SELECT feed_id FROM subscription WHERE app_id=@AppId)",
                    new { AppId = appid });
            }
        }

        [HttpGet, Route("articles"), ModelValidation]
        public object GetArticles([Required]string feedId, int page, int pageCount, string endTime, [Required][FromHeader] string appid)
        {
            if (page < 0 || pageCount <= 0 || string.IsNullOrWhiteSpace(feedId))
            {
                return new StatusCodeResult(204);
            }

            using (var connection = this.helper.GetDbConnection())
            {
                var sql = string.Empty;
                DateTime end = default;
                if (!string.IsNullOrWhiteSpace(endTime) && DateTime.TryParse(endTime, out end))
                {
                    sql = "SELECT * FROM article WHERE feed_id=@FeedId AND published<@EndTime ORDER BY published DESC LIMIT @Skip,@Take";
                }
                else
                {
                    sql = "SELECT * FROM article WHERE feed_id=@FeedId ORDER BY published DESC LIMIT @Skip,@Take";
                }
                return connection.Query<Article>(sql, new
                {
                    FeedId = feedId,
                    Skip = page * pageCount,
                    Take = pageCount,
                    EndTime = end
                });
            }
        }

        [HttpGet, Route("refresh")]
        public object Refresh()
        {
            using (var connection = this.helper.GetDbConnection())
            {
                var feeds = connection.Query<Feed>("SELECT * FROM feed");
                if (feeds != null && feeds.Any())
                {
                    foreach(var feed in feeds)
                    {
                        this.refresher.PushFeed(feed.Url);
                    }
                }
            }

            return true;
        }

        [HttpDelete, Route("feed"), ModelValidation]
        public object DeleteFeed([Required]string feedId, [Required][FromHeader] string appid)
        {
            using (var connection = this.helper.GetDbConnection())
            {
                if (connection.Execute("DELETE FROM subscription WHERE app_id=@AppId AND feed_id=@FeedId", new
                {
                    AppId = appid,
                    FeedId = feedId
                }) > 0)
                {
                    return true;
                }
                else
                {
                    return new StatusCodeResult(500);
                }
            }
        }

        [HttpPost, Route("addfeeds"), ModelValidation]
        public object AddSeeds([Required][FromBody] List<string> feeds, [Required][FromHeader] string appid)
        {
            if (!feeds.Any())
            {
                return new StatusCodeResult(400);
            }

            var counter = new Counter();
            feeds.AsParallel().ForAll(feed =>
            {
                if (this.SubscribeFeed(feed, appid).Title != null)
                {
                    counter.Increment();
                }
            });

            if (counter.Get() > 0)
            {
                return counter.Get();
            }
            else
            {
                return null;
            }
        }

        private (string Title, List<Article> Articles) SubscribeFeed(string feed, string appId)
        {
            try
            {
                SyndicationFeed sf = null;
                String title = null;
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
                    #region 插入 feed
                    var feedDao = new FeedDao(connection);
                    var feedId = feed.Md5();
                    var feedEntity = feedDao.GetFeed(feedId);
                    if (feedEntity == null)
                    {
                        feedEntity = new Rss.Common.Entities.Feed
                        {
                            Id = feedId,
                            Url = feed,
                            Title = title
                        };
                        feedDao.InsertFeed(feedEntity);
                    }
                    #endregion

                    #region 订阅
                    var subscriptionDao = new SubscriptionDao(connection);
                    var subscription = subscriptionDao.GetSubscription(appId, feedId);
                    if (subscription == null)
                    {
                        subscription = new Subscription
                        {
                            AppId = appId,
                            FeedId = feedId
                        };
                        subscriptionDao.InsertSubscription(subscription);
                    }
                    #endregion

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
    }
}
