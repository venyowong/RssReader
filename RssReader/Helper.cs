﻿using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using Niolog;
using Niolog.Interfaces;
using RssReader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace RssReader
{
    public static class Helper
    {
        public static Models.App App { get; set; }

        private static readonly HttpClient _httpClient = new HttpClient();

        static Helper()
        {
            using (var db = new AppDbContext())
            {
                Models.App app = null;
                try
                {
                    app = db.App.FirstOrDefault();
                }
                catch(SqliteException ex)
                {
                    if (ex?.Message?.ToLower().Contains("no such table") ?? false)
                    {
                        db.Database.EnsureDeleted();
                        RelationalDatabaseCreator databaseCreator =
                            (RelationalDatabaseCreator)db.Database.GetService<IDatabaseCreator>();
                        databaseCreator.CreateTables();
                    }
                }
                if (app == null)
                {
                    app = new Models.App
                    {
                        Id = Guid.NewGuid().ToString()
                    };
                    db.Add(app);
                    db.SaveChanges();
                }

                App = app;

                _httpClient.DefaultRequestHeaders.Add("appid", app.Id);
            }

            NiologManager.DefaultWriters = new ILogWriter[]
            {
                new ConsoleLogWriter(),
                new FileLogWriter(Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs"), 10)
            };
        }

        public static T Request<T>(string url, string method)
        {
            try
            {
                string json = null;
                if (method == "GET")
                {
                    json = _httpClient.GetStringAsync(App.BaseUrl + url).Result;
                }
                else if (method == "POST")
                {
                    var response = _httpClient.PostAsync(App.BaseUrl + url, null).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        json = response.Content.ReadAsStringAsync().Result;
                    }
                }

                if (!string.IsNullOrWhiteSpace(json))
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
            }
            catch(Exception e)
            {

            }

            return default;
        }

        public static IAsyncOperation<ContentDialogResult> ShowMessageDialog(string title, string content)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Ok"
            };
            return dialog.ShowAsync();
        }

        public static void EnsureTableExist<T>(DbContext context, DbSet<T> dbSet) where T : class
        {
            try
            {
                dbSet.FirstOrDefault();
            }
            catch(SqliteException e)
            {
                if (e?.Message?.ToLower().Contains("no such table") ?? false)
                {
                    context.Database.EnsureDeleted();
                    RelationalDatabaseCreator databaseCreator =
                        (RelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();
                    databaseCreator.CreateTables();
                }
            }
        }

        public static void ReInit()
        {
            _httpClient.DefaultRequestHeaders.Remove("appid");
            _httpClient.DefaultRequestHeaders.Add("appid", App.Id);
        }

        public static void LogException(Exception e)
        {
            if (e == null)
            {
                return;
            }

            var logger = NiologManager.CreateLogger();
            logger.Error()
                .Exception(e, true)
                .Write();

            ShowMessageDialog("Error", e.Message);
        }

        public static void MarkRead(string url)
        {
            using (var context = new RssDbContext())
            {
                EnsureTableExist(context, context.ReadRecords);
                if (context.ReadRecords.FirstOrDefault(record => record.Url == url) == null)
                {
                    context.ReadRecords.Add(new ReadRecord
                    {
                        Url = url,
                        Time = DateTime.Now
                    });
                    context.SaveChanges();
                }
            }
        }
    }
}