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

namespace RssServer.Controllers
{
    [Route("rss")]
    public class RssController : Controller
    {
        private AppSettings appSettings;

        private static readonly Counter _counter = new Counter();

        private ILogger<RssController> logger;
        
        public RssController(IOptions<AppSettings> options, ILogger<RssController> logger)
        {
            this.appSettings = options?.Value;
            this.logger = logger;
        }

        [HttpPost, Route("add")]
        public object Add(string feed)
        {
            var appid = this.Request.Headers["appid"].ToString();
            if (string.IsNullOrWhiteSpace(appid))
            {
                return new StatusCodeResult(401);
            }

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

                    result.Articles = this.ParseArticles(sf, feedId, connection);
                }

                return result;
            }
            catch(Exception e)
            {
                this.logger.LogError(e, "failed to add");
                return new StatusCodeResult(500);
            }
        }

        [HttpGet, Route("feeds")]
        public object GetFeeds()
        {
            var appid = this.Request.Headers["appid"].ToString();
            if (string.IsNullOrWhiteSpace(appid))
            {
                return new StatusCodeResult(401);
            }

            using (var connection = Helper.GetDbConnection(this.appSettings?.MySql?.ConnectionString))
            {
                return connection.Query<Feed>("SELECT * FROM feed WHERE id IN (SELECT feed_id FROM subscription WHERE app_id=@AppId)",
                    new { AppId = appid });
            }
        }

        [HttpGet, Route("articles")]
        public object GetArticles(string feedId, int page, int pageCount, string endTime)
        {
            var appid = this.Request.Headers["appid"].ToString();
            if (string.IsNullOrWhiteSpace(appid))
            {
                return new StatusCodeResult(401);
            }

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
            if (_counter.Get() <= 0)
            {
                using (var connection = Helper.GetDbConnection(this.appSettings?.MySql?.ConnectionString))
                {
                    var feeds = connection.Query<Feed>("SELECT * FROM feed");
                    if (feeds != null && feeds.Any())
                    {
                        _counter.Reset(feeds.Count());
                        Task.Run(() =>
                        {
                            feeds.AsParallel().ForAll(feed =>
                            {
                                using (var connection = Helper.GetDbConnection(this.appSettings?.MySql?.ConnectionString))
                                {
                                    var sf = SyndicationFeed.Load(XmlReader.Create(feed.Url));
                                    this.ParseArticles(sf, feed.Url.Md5(), connection);
                                    _counter.Decrement();
                                }
                            });
                        });
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private string Simplify(string input, int length = 500)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            if (input.Length <= length)
            {
                return input;
            }

            return input.Substring(0, length - 3) + "...";
        }

        private List<Article> ParseArticles(SyndicationFeed sf, string feedId, IDbConnection connection)
        {
            if (sf == null || sf.Items == null || !sf.Items.Any())
            {
                this.logger.LogWarning("SyndicationFeed is empty, so no articles.");
                return null;
            }

            var articles = new List<Article>();
            foreach (var item in sf.Items)
            {
                var articleUrl = item?.Links?.FirstOrDefault()?.Uri?.AbsoluteUri;
                var articleTitle = item?.Title?.Text;
                if (string.IsNullOrWhiteSpace(articleUrl) || string.IsNullOrWhiteSpace(articleTitle))
                {
                    continue;
                }

                var articleId = feedId + articleUrl.Md5();
                var content = string.Empty;
                if (item.Content is TextSyndicationContent textContent)
                {
                    content = textContent.Text;
                }
                Article article = new Article
                {
                    Id = articleId,
                    Url = articleUrl,
                    FeedId = feedId,
                    Title = articleTitle,
                    Summary = this.Simplify(item.Summary?.Text),
                    Published = item.PublishDate.LocalDateTime,
                    Updated = item.LastUpdatedTime.LocalDateTime,
                    Keyword = string.Join(',', item.Categories?.Select(c => c?.Name)),
                    Content = this.Simplify(content),
                    Contributors = string.Join(',', item.Contributors?.Select(c => c?.Name)),
                    Authors = string.Join(',', item.Authors?.Select(c => c?.Name)),
                    Copyright = item.Copyright?.Text
                };
                articles.Add(article);

                if (connection.QueryFirstOrDefault<Article>("SELECT * FROM article WHERE id=@Id", new { Id = articleId }) == null)
                {
                    connection.Execute("INSERT INTO article(id, url, feed_id, title, summary, published, updated, created, keyword, content, contributors, " +
                        "authors, copyright) VALUES(@Id, @Url, @FeedId, @Title, @Summary, @Published, @Updated, now(), @Keyword, @Content, " +
                        "@Contributors, @Authors, @Copyright)", article);
                }
                else
                {
                    connection.Execute("UPDATE article SET title=@Title, summary=@Summary, published=@Published, updated=@Updated, " +
                        "keyword=@Keyword, content=@Content, contributors=@Contributors, authors=@Authors, copyright=@Copyright WHERE id=@Id", article);
                }
            }

            return articles;
        }
    }
}
