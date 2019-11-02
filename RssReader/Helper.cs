using Newtonsoft.Json;
using Niolog;
using Niolog.Interfaces;
using RssReader.Daos;
using RssReader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace RssReader
{
    public static class Helper
    {
        public static Application App { get; set; }

        private static readonly HttpClient _httpClient = new HttpClient();

        static Helper()
        {
            var appDao = new AppDao();
            App = appDao.GetApp();
            if (App == null)
            {
                App = new Application
                {
                    Id = Guid.NewGuid().ToString()
                };
                appDao.InsertApp(App);
            }
            _httpClient.DefaultRequestHeaders.Add("appid", App.Id);

            NiologManager.DefaultWriters = new ILogWriter[]
            {
                new ConsoleLogWriter(),
                new FileLogWriter(Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs"), 10)
            };
        }

        public static T Request<T>(string url, string method, object obj = null)
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
                    StringContent content = null;
                    if (obj != null)
                    {
                        content = new StringContent(JsonConvert.SerializeObject(obj));
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    }
                    var response = _httpClient.PostAsync(App.BaseUrl + url, content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        json = response.Content.ReadAsStringAsync().Result;
                    }
                }
                else if (method == "DELETE")
                {
                    var response = _httpClient.DeleteAsync(App.BaseUrl + url).Result;
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
            var rssDao = new RssDao();
            rssDao.InsertReadRecord(new ReadRecord
            {
                Url = url,
                Time = DateTime.Now
            });
        }

        public static INiologger GetLogger()
        {
            return NiologManager.CreateLogger();
        }
    }
}
