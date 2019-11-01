using System.Data;
using Dapper;
using Rss.Common.Entities;

namespace RssServer.Daos
{
    public class ArticleDao
    {
        private IDbConnection connection;

        public ArticleDao(IDbConnection connection)
        {
            this.connection = connection;
        }

        public Article GetArticle(string articleId)
        {
            return connection.QueryFirstOrDefault<Article>("SELECT * FROM article WHERE id=@Id", new { Id = articleId });
        }

        public int InsertArticle(Article article)
        {
            if (article == null)
            {
                return 0;
            }

            return connection.Execute("INSERT INTO article(id, url, feed_id, title, summary, published, updated, created, keyword, content, contributors, " +
                "authors, copyright) VALUES(@Id, @Url, @FeedId, @Title, @Summary, @Published, @Updated, now(), @Keyword, @Content, " +
                "@Contributors, @Authors, @Copyright)", article);
        }

        public int UpdateArticle(Article article)
        {
            if (article == null)
            {
                return 0;
            }

            return connection.Execute("UPDATE article SET title=@Title, summary=@Summary, published=@Published, updated=@Updated, " +
                "keyword=@Keyword, content=@Content, contributors=@Contributors, authors=@Authors, copyright=@Copyright WHERE id=@Id", article);
        }
    }
}