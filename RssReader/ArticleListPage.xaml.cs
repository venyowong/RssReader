using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Rss.Common.Entities;
using RssReader.Models;
using RssReader.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RssReader
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ArticleListPage : Page
    {
        private static readonly Regex _ignoreTagRegex = new Regex("<[^>]*>");

        private DateTime endTime = DateTime.Now;

        private string feedId = null;

        public RssViewModel RssModel { get; set; } = new RssViewModel();

        public ArticleListPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.feedId = e.Parameter.ToString();
            var articles = Helper.Request<List<Article>>($"/rss/articles?feedId={e.Parameter}&page=0&pageCount=30", "GET");
            this.RssModel.Articles = this.GetUnreadArticles(articles);
        }

        private void ArticleListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ArticleViewModel article)
            {
                article.Icon = new SymbolIcon((Symbol)0xE73E);
                using (var context = new RssDbContext())
                {
                    Helper.EnsureTableExist(context, context.ReadRecords);
                    context.ReadRecords.Add(new ReadRecord
                    {
                        Url = article.Url,
                        Time = DateTime.Now
                    });
                    context.SaveChanges();
                }
                Windows.System.Launcher.LaunchUriAsync(new Uri(article.Url));
            }
        }

        private void ReadAndLoadButton_Click(object sender, RoutedEventArgs e)
        {
            this.RssModel.Articles.ForEach(a => a.Icon = new SymbolIcon((Symbol)0xE73E));

            using(var context = new RssDbContext())
            {
                Helper.EnsureTableExist(context, context.ReadRecords);
            }
            this.RssModel.Articles.AsParallel().ForAll(article =>
            {
                using (var context = new RssDbContext())
                {
                    context.ReadRecords.Add(new ReadRecord
                    {
                        Url = article.Url,
                        Time = DateTime.Now
                    });
                }
            });

            var articles = Helper.Request<List<Article>>($"/rss/articles?feedId={this.feedId}&page=0&pageCount=30&endTime={this.endTime}", "GET");
            if (articles == null || !articles.Any())
            {
                Helper.ShowMessageDialog("Message", "No more articles.");
                return;
            }

            this.CombineArticleList(this.RssModel.Articles, this.GetUnreadArticles(articles));
            this.RssModel.OnPropertyChanged("Articles");
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var articles = Helper.Request<List<Article>>($"/rss/articles?feedId={this.feedId}&page=0&pageCount=30&endTime={this.endTime}", "GET");
            if (articles == null || !articles.Any())
            {
                Helper.ShowMessageDialog("Message", "No more articles.");
                return;
            }

            this.CombineArticleList(this.RssModel.Articles, this.GetUnreadArticles(articles));
            this.RssModel.OnPropertyChanged("Articles");
        }

        private void PullButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ArticleListPage), this.feedId);
        }

        private ArticleViewModel ConvertArticle(Article article)
        {
            if (article == null)
            {
                return null;
            }

            if (article.Published < this.endTime)
            {
                this.endTime = article.Published;
            }

            var articleViewModel = new ArticleViewModel
            {
                Id = article.Id,
                Authors = article.Authors,
                Contributors = article.Contributors,
                Copyright = article.Copyright,
                Created = article.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                FeedId = article.FeedId,
                Keyword = article.Keyword,
                Published = article.Published.ToString("yyyy-MM-dd HH:mm:ss"),
                Title = article.Title,
                Updated = article.Updated.ToString("yyyy-MM-dd HH:mm:ss"),
                Url = article.Url
            };
            if (!string.IsNullOrWhiteSpace(article.Summary))
            {
                articleViewModel.Summary = _ignoreTagRegex.Replace(article.Summary, " ");
            }
            if (!string.IsNullOrWhiteSpace(article.Content))
            {
                articleViewModel.Content = _ignoreTagRegex.Replace(article.Content, " ");
            }
            return articleViewModel;
        }

        private void CombineArticleList(List<ArticleViewModel> list, List<ArticleViewModel> additional)
        {
            if (additional == null || !additional.Any())
            {
                return;
            }

            foreach(var article in additional)
            {
                if (!list.Any(a => a.Url == article.Url))
                {
                    list.Add(article);
                }
            }
        }

        private List<ArticleViewModel> GetUnreadArticles(List<Article> articles)
        {
            if (articles == null || !articles.Any())
            {
                return null;
            }

            using (var context = new RssDbContext())
            {
                Helper.EnsureTableExist(context, context.ReadRecords);
            }
            return articles.AsParallel()
                .AsOrdered()
                .Where(article =>
                {
                    using (var context = new RssDbContext())
                    {
                        return context.ReadRecords.FirstOrDefault(record => record.Url == article.Url) == null;
                    }
                })
                .Select(article => this.ConvertArticle(article)).ToList();
        }
    }
}
