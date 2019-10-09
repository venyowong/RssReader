using Rss.Common.Entities;
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
            { "import", typeof(ImportPage) }
        };
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            this.NavView.SelectedItem = this.NavView.MenuItems[0];
            this.NavView_Navigate("home", new EntranceNavigationTransitionInfo());
            this.RefreshFeeds();
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
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

        private void NavView_Navigate(string navItemTag, NavigationTransitionInfo transitionInfo, object param = null)
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
                this.ContentFrame.Navigate(typeof(ArticleListPage), navItemTag, transitionInfo);
            }
        }

        private List<NavigationViewItem> feedItems = new List<NavigationViewItem>();
        private void RefreshFeeds()
        {
            var feeds = Helper.Request<List<Feed>>("/rss/feeds", "GET");
            if (feeds != null && feeds.Any())
            {
                this.feedItems.ForEach(item => this.NavView.MenuItems.Remove(item));
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
    }
}
