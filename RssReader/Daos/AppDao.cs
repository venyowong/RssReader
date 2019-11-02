using LiteDB;
using RssReader.Models;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace RssReader.Daos
{
    public class AppDao
    {
        public BsonValue InsertApp(Application app)
        {
            using(var db = this.GetLiteDatabase())
            {
                var appCollection = db.GetCollection<Application>("app");
                return appCollection.Insert(app);
            }
        }

        public Application GetApp()
        {
            using (var db = this.GetLiteDatabase())
            {
                var appCollection = db.GetCollection<Application>("app");
                appCollection.EnsureIndex(app => app.Id);
                return appCollection.FindAll().FirstOrDefault();
            }
        }

        public int DeleteApp()
        {
            using (var db = this.GetLiteDatabase())
            {
                var appCollection = db.GetCollection<Application>("app");
                return appCollection.Delete(_ => true);
            }
        }

        private LiteDatabase GetLiteDatabase()
        {
            return new LiteDatabase(Path.Combine(ApplicationData.Current.LocalFolder.Path, "app.ldb"));
        }
    }
}
