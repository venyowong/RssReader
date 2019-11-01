using System.Data;
using Dapper;
using Rss.Common.Entities;

namespace RssServer.Daos
{
    public class FeedDao
    {
        private IDbConnection connection;

        public FeedDao(IDbConnection connection)
        {
            this.connection = connection;
        }

        public Feed GetFeed(string feedId)
        {
            return connection.QueryFirstOrDefault<Feed>("SELECT * FROM feed WHERE id=@Id", new { Id = feedId });
        }

        public int InsertFeed(Feed feed)
        {
            if (feed == null)
            {
                return 0;
            }
            return connection.Execute("INSERT INTO feed(id, url, title) VALUES(@Id, @Url, @Title)", feed);
        }
    }
}