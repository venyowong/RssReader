using Rss.Common.Entities;
using Rss.Common.Models;
using RssReader.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
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
    public sealed partial class AddPage : Page
    {
        private static readonly Regex _ignoreTagRegex = new Regex("<[^>]*>");
        public RssViewModel RssModel { get; set; } = new RssViewModel();

        public AddPage()
        {
            this.InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.RssUriText.Text))
                {
                    return;
                }

                var rssModel = Helper.Request<RssModel>("/rss/add?feed=" + HttpUtility.UrlEncode(this.RssUriText.Text), "POST");
                if (string.IsNullOrWhiteSpace(rssModel?.Title))
                {
                    Helper.ShowMessageDialog("Warn", "Cannot parse this feed.");
                    return;
                }

                this.RssModel.Title = rssModel.Title;
                this.RssModel.Articles = rssModel.Articles?.AsParallel().AsOrdered().Select(article =>
                {
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
                }).ToList();
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
            }
        }

        private void ArticleListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Article article)
            {
                Windows.System.Launcher.LaunchUriAsync(new Uri(article.Url));
            }
        }
    }
}
