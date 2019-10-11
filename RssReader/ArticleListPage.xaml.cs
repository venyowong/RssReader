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

        private DateTime endTime;

        private string feedId = null;

        public RssViewModel RssModel { get; set; } = new RssViewModel();

        public ArticleListPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                base.OnNavigatedTo(e);

                if (e.Parameter != null && e.Parameter is ArticleListPageParams parameters)
                {
                    this.feedId = parameters.FeedId;
                    this.endTime = parameters.EndTime;
                    var url = $"/rss/articles?feedId={this.feedId}&page=0&pageCount=30";
                    if (this.endTime != default)
                    {
                        url = $"/rss/articles?feedId={this.feedId}&page=0&pageCount=30&endTime={this.endTime}";
                    }
                    var articles = Helper.Request<List<Article>>(url, "GET");
                    if (articles == null || !articles.Any())
                    {
                        Helper.ShowMessageDialog("Message", "No more articles.");
                        return;
                    }

                    this.RssModel.Articles = this.GetUnreadArticles(articles);
                }
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
            }
        }

        private void ArticleListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                if (e.ClickedItem is ArticleViewModel article)
                {
                    if (article.Icon == null)
                    {
                        article.Icon = new SymbolIcon((Symbol)0xE73E);
                    }
                    Helper.MarkRead(article.Url);
                    Windows.System.Launcher.LaunchUriAsync(new Uri(article.Url));
                }
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
            }
        }

        private void ReadAndLoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.RssModel.Articles?.AsParallel().ForAll(article => Helper.MarkRead(article.Url));

                this.Frame.Navigate(typeof(ArticleListPage), new ArticleListPageParams
                {
                    FeedId = this.feedId,
                    EndTime = this.endTime
                });
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var articles = Helper.Request<List<Article>>($"/rss/articles?feedId={this.feedId}&page=0&pageCount=30&endTime={this.endTime}", "GET");
                if (articles == null || !articles.Any())
                {
                    Helper.ShowMessageDialog("Message", "No more articles.");
                    return;
                }

                this.RssModel.Articles = this.CombineArticleList(this.RssModel.Articles, this.GetUnreadArticles(articles));
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
            }
        }

        private void PullButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ArticleListPage), new ArticleListPageParams
            {
                FeedId = this.feedId,
                EndTime = this.endTime
            });
        }

        private ArticleViewModel ConvertArticle(Article article, bool readed = false)
        {
            if (article == null)
            {
                return null;
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
                Url = article.Url,
                Icon = readed ? new SymbolIcon((Symbol)0xE73E) : null
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

        private List<ArticleViewModel> CombineArticleList(List<ArticleViewModel> list, List<ArticleViewModel> additional)
        {
            List<ArticleViewModel> newList = new List<ArticleViewModel>();
            if (list != null)
            {
                newList.AddRange(list);
            }

            if (additional == null || !additional.Any())
            {
                return newList;
            }

            foreach(var article in additional)
            {
                if (!newList.Any(a => a.Url == article.Url))
                {
                    newList.Add(article);
                }
            }
            return newList;
        }

        /// <summary>
        /// 获取未读文章，若所有文章都已读，则返回所有已读文章
        /// </summary>
        /// <param name="articles"></param>
        /// <returns></returns>
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
            var unreadArticles = articles.OrderByDescending(a => a.Published).AsParallel()
                .AsOrdered()
                .Where(article =>
                {
                    if (this.endTime == default || article.Published < this.endTime)
                    {
                        this.endTime = article.Published;
                    }

                    using (var context = new RssDbContext())
                    {
                        return context.ReadRecords.FirstOrDefault(record => record.Url == article.Url) == null;
                    }
                });
            if (unreadArticles.Any())
            {
                return unreadArticles.Select(article => this.ConvertArticle(article)).ToList();
            }
            else
            {
                return articles.Select(article => this.ConvertArticle(article, true)).ToList();
            }
        }
    }
}
