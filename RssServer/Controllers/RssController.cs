using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Dapper;
using System.Linq;
using Rss.Common.Models;
using Rss.Common.Entities;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace RssServer.Controllers
{
    [Route("rss")]
    public class RssController : Controller
    {
        private AppSettings appSettings;

        private ILogger<RssController> logger;

        private RssRefresher refresher;

        public RssController(IOptions<AppSettings> options, ILogger<RssController> logger, RssRefresher refresher)
        {
            this.appSettings = options?.Value;
            this.logger = logger;
            this.refresher = refresher;
        }

        [HttpPost, Route("add"), ModelValidation]
        public object Add([Required]string feed, [Required][FromHeader] string appid)
        {
            try
            {
                var sf = SyndicationFeed.Load(XmlReader.Create(feed));
                var title = sf?.Title?.Text;
                if (string.IsNullOrWhiteSpace(title))
                {
                    this.logger.LogWarning($"The title of feed({feed}) is null or white space.");
                    return string.Empty;
                }

                var result = new RssModel
                {
                    Title = title
                };

                using (var connection = Helper.GetDbConnection(this.appSettings?.MySql?.ConnectionString))
                {
                    #region 插入 feed
                    var feedId = feed.Md5();
                    var feedEntity = connection.QueryFirstOrDefault<Feed>("SELECT * FROM feed WHERE id=@Id", new { Id = feedId });
                    if (feedEntity == null)
                    {
                        feedEntity = new Feed
                        {
                            Id = feedId,
                            Url = feed,
                            Title = title
                        };
                        connection.Execute("INSERT INTO feed(id, url, title) VALUES(@Id, @Url, @Title)", feedEntity);
                    }
                    #endregion

                    #region 订阅
                    var subscription = connection.QueryFirstOrDefault<Subscription>(
                        "SELECT * FROM subscription WHERE feed_id=@FeedId AND app_id=@AppId", new { FeedId = feedId, AppId = appid });
                    if (subscription == null)
                    {
                        subscription = new Subscription
                        {
                            AppId = appid,
                            FeedId = feedId
                        };
                        connection.Execute("INSERT INTO subscription(app_id, feed_id) VALUES(@AppId, @FeedId)", subscription);
                    }
                    #endregion

                    result.Articles = Helper.ParseArticles(sf, feedId, connection);
                }

                return result;
            }
            catch(Exception e)
            {
                this.logger.LogError(e, "failed to add");
                return new StatusCodeResult(500);
            }
        }

        [HttpGet, Route("feeds"), ModelValidation]
        public object GetFeeds([Required][FromHeader] string appid)
        {
            using (var connection = Helper.GetDbConnection(this.appSettings?.MySql?.ConnectionString))
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

            using (var connection = Helper.GetDbConnection(this.appSettings?.MySql?.ConnectionString))
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
            using (var connection = Helper.GetDbConnection(this.appSettings?.MySql?.ConnectionString))
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
            using (var connection = Helper.GetDbConnection(this.appSettings?.MySql?.ConnectionString))
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
                try
                {
                    var sf = SyndicationFeed.Load(XmlReader.Create(feed));
                    var title = sf?.Title?.Text;
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        this.logger.LogWarning($"The title of feed({feed}) is null or white space.");
                        return;
                    }

                    counter.Increment();
                    using (var connection = Helper.GetDbConnection(this.appSettings?.MySql?.ConnectionString))
                    {
                        #region 插入 feed
                        var feedId = feed.Md5();
                        var feedEntity = connection.QueryFirstOrDefault<Feed>("SELECT * FROM feed WHERE id=@Id", new { Id = feedId });
                        if (feedEntity == null)
                        {
                            feedEntity = new Feed
                            {
                                Id = feedId,
                                Url = feed,
                                Title = title
                            };
                            connection.Execute("INSERT INTO feed(id, url, title) VALUES(@Id, @Url, @Title)", feedEntity);
                        }
                        #endregion

                        #region 订阅
                        var subscription = connection.QueryFirstOrDefault<Subscription>(
                            "SELECT * FROM subscription WHERE feed_id=@FeedId", new { FeedId = feedId });
                        if (subscription == null)
                        {
                            subscription = new Subscription
                            {
                                AppId = appid,
                                FeedId = feedId
                            };
                            connection.Execute("INSERT INTO subscription(app_id, feed_id) VALUES(@AppId, @FeedId)", subscription);
                        }
                        #endregion

                        Helper.ParseArticles(sf, feedId, connection);
                    }
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, $"failed to add {feed}");
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
    }
}
