using Dapper;
using MySql.Data.MySqlClient;
using Niolog;
using Rss.Common.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;

namespace RssServer
{
    public static class Helper
    {
        public static IDbConnection GetDbConnection(string connectionString)
        {
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        public static string Md5(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            using (MD5 md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }

        public static string Simplify(string input, int length = 500)
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

        public static List<Article> ParseArticles(SyndicationFeed sf, string feedId, IDbConnection connection)
        {
            if (sf == null || sf.Items == null || !sf.Items.Any())
            {
                NiologManager.CreateLogger().Warn()
                    .Message("SyndicationFeed is empty, so no articles.")
                    .Write();
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
