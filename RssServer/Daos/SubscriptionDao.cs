using System.Data;
using Dapper;
using Rss.Common.Entities;

namespace RssServer.Daos
{
    public class SubscriptionDao
    {
        private IDbConnection connection;

        public SubscriptionDao(IDbConnection connection)
        {
            this.connection = connection;
        }

        public Subscription GetSubscription(string appId, string feedId)
        {
            return connection.QueryFirstOrDefault<Subscription>(
                "SELECT * FROM subscription WHERE feed_id=@FeedId AND app_id=@AppId", new { FeedId = feedId, AppId = appId });
        }

        public int InsertSubscription(Subscription subscription)
        {
            if (subscription == null)
            {
                return 0;
            }

            return connection.Execute("INSERT INTO subscription(app_id, feed_id) VALUES(@AppId, @FeedId)", subscription);
        }
    }
}