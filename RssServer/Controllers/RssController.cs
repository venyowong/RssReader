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
using System.Text.RegularExpressions;
using RssServer.User.Interfaces;

namespace RssServer.Controllers
{
    [Route("rss")]
    public class RssController : Controller
    {
        private ILogger<RssController> logger;

        private RssFetcher fetcher;

        private Helper helper;

        private IUserService userService;

        private Regex appIdRegex = new Regex("user_(.*)");

        public RssController(Helper helper, ILogger<RssController> logger, RssFetcher fetcher,
            IUserService userService)
        {
            this.helper = helper;
            this.logger = logger;
            this.fetcher = fetcher;
            this.userService = userService;
        }

        [HttpPost, Route("add"), ModelValidation]
        public object Add([Required]string feed, [Required][FromHeader] string appid)
        {
            if (!this.VerifyAppId(appid))
            {
                return new StatusCodeResult(401);
            }

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
            if (!this.VerifyAppId(appid))
            {
                return new StatusCodeResult(401);
            }

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
            if (!this.VerifyAppId(appid))
            {
                return new StatusCodeResult(401);
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

        [HttpDelete, Route("feed"), ModelValidation]
        public object DeleteFeed([Required]string feedId, [Required][FromHeader] string appid)
        {
            if (!this.VerifyAppId(appid))
            {
                return new StatusCodeResult(401);
            }

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
            if (!this.VerifyAppId(appid))
            {
                return new StatusCodeResult(401);
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
            var result = this.fetcher.Fetch(feed);
            if (result == default)
            {
                return result;
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
                        Title = result.Title
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
            }

            return result;
        }
    
        private bool VerifyAppId(string appId)
        {
            if (this.userService == null)
            {
                return false;
            }

            var match = this.appIdRegex.Match(appId);
            if (!match.Success)
            {
                return true;
            }

            var token = this.Request.Headers["access-token"];
            var userId = match.Groups[1].Value;
            return this.userService.VerifyToken(userId, token);
        }
    }
}
