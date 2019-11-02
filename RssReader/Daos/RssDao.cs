using LiteDB;
using RssReader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace RssReader.Daos
{
    public class RssDao
    {
        public BsonValue InsertReadRecord(ReadRecord record)
        {
            using (var db = this.GetLiteDatabase())
            {
                var collection = db.GetCollection<ReadRecord>("readrecord");
                return collection.Insert(record);
            }
        }

        public ReadRecord GetReadRecord(string url)
        {
            using (var db = this.GetLiteDatabase())
            {
                var collection = db.GetCollection<ReadRecord>("readrecord");
                return collection.FindOne(record => record.Url == url);
            }
        }

        private LiteDatabase GetLiteDatabase()
        {
            return new LiteDatabase(Path.Combine(ApplicationData.Current.LocalFolder.Path, "rss.ldb"));
        }
    }
}
