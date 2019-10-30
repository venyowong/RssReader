using Rss.Common.Entities;
using RssReader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace RssReader
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static readonly Dictionary<string, Type> _pages = new Dictionary<string, Type>
        {
            { "home", typeof(HomePage) },
            { "add", typeof(AddPage) },
            { "import", typeof(ImportPage) },
            { "refreshfeeds", null },
            { "export", typeof(ExportPage) }
        };

        private MenuFlyout feedMenuFlyout;

        public MainPage()
        {
            this.InitializeComponent();
            this.feedMenuFlyout = (MenuFlyout)this.NavView.Resources["FeedContextMenu"];
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.NavView.SelectedItem = this.NavView.MenuItems[0];
                this.NavView_Navigate("home", new EntranceNavigationTransitionInfo());
                this.RefreshFeeds();
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            try
            {
                if (args.IsSettingsInvoked)
                {
                    this.ContentFrame.Navigate(typeof(SettingsPage), args.RecommendedNavigationTransitionInfo);
                    return;
                }

                if (args.InvokedItemContainer != null)
                {
                    var navItemTag = args.InvokedItemContainer.Tag.ToString();
                    NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
                }
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
            }
        }

        private void NavView_Navigate(string navItemTag, NavigationTransitionInfo transitionInfo, object param = null)
        {
            try
            {
                if (navItemTag == "refreshfeeds")
                {
                    this.RefreshFeeds();
                    return;
                }

                Type page = null;
                if (_pages.ContainsKey(navItemTag))
                {
                    page = _pages[navItemTag];
                }

                if (!(page is null))
                {
                    this.ContentFrame.Navigate(page, param, transitionInfo);
                }
                else
                {
                    this.ContentFrame.Navigate(typeof(ArticleListPage), new ArticleListPageParams
                    {
                        FeedId = navItemTag
                    }, transitionInfo);
                }
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
            }
        }

        private List<NavigationViewItem> feedItems = new List<NavigationViewItem>();
        private void RefreshFeeds()
        {
            var feeds = Helper.Request<List<Feed>>("/rss/feeds", "GET");
            if (feeds != null && feeds.Any())
            {
                this.feedItems.ForEach(item => this.NavView.MenuItems.Remove(item));
                this.feedItems.Clear();
                feeds.ForEach(feed =>
                {
                    var item = new NavigationViewItem
                    {
                        Tag = feed.Id,
                        Content = feed.Title,
                        Icon = new SymbolIcon((Symbol)0xE8A4)
                    };
                    this.feedItems.Add(item);
                    this.NavView.MenuItems.Add(item);
                });
            }
        }

        private void NavView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var requestedElement = (FrameworkElement)args.OriginalSource;
            while ((requestedElement != sender) && !(requestedElement is NavigationViewItem))
            {
                requestedElement = (FrameworkElement)VisualTreeHelper.GetParent(requestedElement);
            }
            if (requestedElement != sender && !_pages.ContainsKey(requestedElement.Tag.ToString()))
            {
                if (args.TryGetPosition(requestedElement, out Point point))
                {
                    this.feedMenuFlyout.ShowAt(requestedElement, point);
                }
                else
                {
                    // Not invoked via pointer, so let XAML choose a default location.
                    this.feedMenuFlyout.ShowAt(requestedElement);
                }

                args.Handled = true;
            }
        }

        private void NavView_ContextCanceled(UIElement sender, RoutedEventArgs args)
        {
            this.feedMenuFlyout.Hide();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var feedId = this.feedMenuFlyout.Target.Tag.ToString();
            if (!string.IsNullOrWhiteSpace(Helper.Request<string>($"/rss/feed?feedId={feedId}", "DELETE")))
            {
                this.feedItems.Remove((NavigationViewItem)this.feedMenuFlyout.Target);
                this.NavView.MenuItems.Remove(this.feedMenuFlyout.Target);
            }
        }
    }
}
