using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Niolog;
using Rss.Common.Entities;
using RssServer.Daos;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;

namespace RssServer.Helpers
{
    public class Helper
    {
        private AppSettings appSettings;

        public Helper(IOptions<AppSettings> options)
        {
            this.appSettings = options?.Value;
        }

        public IDbConnection GetDbConnection()
        {
            var connection = new MySqlConnection(this.appSettings?.MySql?.ConnectionString);
            connection.Open();
            return connection;
        }

        public string Simplify(string input, int length = 500)
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

        public List<Article> ParseArticles(SyndicationFeed sf, string feedId, IDbConnection connection)
        {
            if (sf == null || sf.Items == null || !sf.Items.Any())
            {
                NiologManager.CreateLogger().Warn()
                    .Message("SyndicationFeed is empty, so no articles.")
                    .Write();
                return null;
            }

            var articleDao = new ArticleDao(connection);
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
                    Summary = Simplify(item.Summary?.Text),
                    Published = item.PublishDate.LocalDateTime,
                    Updated = item.LastUpdatedTime.LocalDateTime,
                    Keyword = string.Join(',', item.Categories?.Select(c => c?.Name)),
                    Content = Simplify(content),
                    Contributors = string.Join(',', item.Contributors?.Select(c => c?.Name)),
                    Authors = string.Join(',', item.Authors?.Select(c => c?.Name)),
                    Copyright = item.Copyright?.Text
                };
                articles.Add(article);

                try
                {
                    if (articleDao.GetArticle(articleId) == null)
                    {
                        articleDao.InsertArticle(article);
                    }
                    else
                    {
                        articleDao.UpdateArticle(article);
                    }
                }
                catch(Exception e)
                {
                    NiologManager.CreateLogger().Error()
                        .Message($"error occured when insert {articleUrl}")
                        .Exception(e)
                        .Write();
                }
            }

            return articles;
        }

        public List<Article> ParseArticles(CodeHollow.FeedReader.Feed feed, string feedId, IDbConnection connection)
        {
            if (feed == null || feed.Items == null || !feed.Items.Any())
            {
                NiologManager.CreateLogger().Warn()
                    .Message("Feed is empty, so no articles.")
                    .Write();
                return null;
            }

            var articleDao = new ArticleDao(connection);
            var articles = new List<Article>();
            foreach (var item in feed.Items)
            {
                var articleUrl = item?.Link;
                var articleTitle = item?.Title;
                if (string.IsNullOrWhiteSpace(articleUrl) || string.IsNullOrWhiteSpace(articleTitle))
                {
                    continue;
                }

                var articleId = feedId + articleUrl.Md5();
                var content = this.Simplify(item.Content);
                Article article = new Article
                {
                    Id = articleId,
                    Url = articleUrl,
                    FeedId = feedId,
                    Title = articleTitle,
                    Summary = content,
                    Published = item.PublishingDate ?? DateTime.Now,
                    Updated = item.PublishingDate ?? DateTime.Now,
                    Keyword = string.Join(',', item.Categories),
                    Content = content,
                    Contributors = item.Author,
                    Authors = item.Author
                };
                articles.Add(article);

                try
                {
                    if (articleDao.GetArticle(articleId) == null)
                    {
                        articleDao.InsertArticle(article);
                    }
                    else
                    {
                        articleDao.UpdateArticle(article);
                    }
                }
                catch(Exception e)
                {
                    NiologManager.CreateLogger().Error()
                        .Message($"error occured when insert {articleUrl}")
                        .Exception(e)
                        .Write();
                }
            }

            return articles;
        }
    }
}
