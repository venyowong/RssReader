using Niolog;
using Rss.Common.Entities;
using RssReader.Daos;
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

        private ArticleListPageParams parameters;

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
                    this.parameters = parameters;
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

                    this.RssModel.Articles = this.GetUnreadArticles(articles, parameters.ArticleId);
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
                    if (this.parameters != null)
                    {
                        this.parameters.ArticleId = article.Id;
                        DateTime.TryParse(article.Published, out DateTime published);
                        this.parameters.EndTime = published.AddSeconds(1);
                    }
                    if (Helper.App.UseWebView)
                    {
                        this.Frame.Navigate(typeof(WebPage), new WebPageParams
                        {
                            Url = article.Url
                        });
                    }
                    else
                    {
                        Windows.System.Launcher.LaunchUriAsync(new Uri(article.Url));
                    }
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
                this.RssModel.Articles?.ForEach(article => Helper.MarkRead(article.Url));

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
                FeedId = this.feedId
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
        private List<ArticleViewModel> GetUnreadArticles(List<Article> articles, string articleId = null)
        {
            if (articles == null || !articles.Any())
            {
                return null;
            }

            var unreadArticles = articles.OrderByDescending(a => a.Published)
                .Where(article =>
                {
                    if (this.endTime == default || article.Published < this.endTime)
                    {
                        this.endTime = article.Published;
                    }

                    if (articleId == article.Id)
                    {
                        return true;
                    }

                    var rssDao = new RssDao();
                    return rssDao.GetReadRecord(article.Url) == null;
                });
            if (unreadArticles.Any())
            {
                var result = unreadArticles.Select(article => this.ConvertArticle(article)).ToList();
                result.ForEach(m =>
                {
                    if (m.Id == articleId && m.Icon == null)
                    {
                        if (m.Icon == null)
                        {
                            m.Icon = new SymbolIcon((Symbol)0xE73E);
                        }
                    }
                });
                return result;
            }
            else
            {
                return articles.Select(article => this.ConvertArticle(article, true)).ToList();
            }
        }
    }
}
