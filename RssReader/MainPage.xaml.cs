using Rss.Common.Entities;
using RssReader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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

        private MenuFlyout FeedMenuFlyout;

        public MainPage()
        {
            this.InitializeComponent();
            this.FeedMenuFlyout = (MenuFlyout)this.NavView.Resources["FeedContextMenu"];
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.NavView.SelectedItem = this.NavView.MenuItems[0];
                this.NavView_Navigate("home", new EntranceNavigationTransitionInfo());
                this.RefreshFeeds();

                // Add keyboard accelerators for backwards navigation.
                var goBack = new KeyboardAccelerator { Key = VirtualKey.GoBack };
                goBack.Invoked += BackInvoked;
                this.KeyboardAccelerators.Add(goBack);

                // ALT routes here
                var altLeft = new KeyboardAccelerator
                {
                    Key = VirtualKey.Left,
                    Modifiers = VirtualKeyModifiers.Menu
                };
                altLeft.Invoked += BackInvoked;
                this.KeyboardAccelerators.Add(altLeft);

                // Add handler for ContentFrame navigation.
                ContentFrame.Navigated += On_Navigated;
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
                    this.FeedMenuFlyout.ShowAt(requestedElement, point);
                }
                else
                {
                    // Not invoked via pointer, so let XAML choose a default location.
                    this.FeedMenuFlyout.ShowAt(requestedElement);
                }

                args.Handled = true;
            }
        }

        private void NavView_ContextCanceled(UIElement sender, RoutedEventArgs args)
        {
            this.FeedMenuFlyout.Hide();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var feedId = this.FeedMenuFlyout.Target.Tag.ToString();
            if (!string.IsNullOrWhiteSpace(Helper.Request<string>($"/rss/feed?feedId={feedId}", "DELETE")))
            {
                this.feedItems.Remove((NavigationViewItem)this.FeedMenuFlyout.Target);
                this.NavView.MenuItems.Remove(this.FeedMenuFlyout.Target);
            }
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            On_BackRequested();
        }

        private void BackInvoked(KeyboardAccelerator sender,
                                 KeyboardAcceleratorInvokedEventArgs args)
        {
            On_BackRequested();
            args.Handled = true;
        }

        private bool On_BackRequested()
        {
            if (ContentFrame.SourcePageType == typeof(WebPage) && ContentFrame.Content is WebPage webPage
                && webPage.GoBackWebView())
            {
                return true;
            }

            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (NavView.IsPaneOpen &&
                (NavView.DisplayMode == NavigationViewDisplayMode.Compact ||
                 NavView.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
                NavView.SelectedItem = (NavigationViewItem)NavView.SettingsItem;
                NavView.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                var item = _pages.FirstOrDefault(p => p.Value == e.SourcePageType);
                var tag = string.Empty;
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    if (e.SourcePageType == typeof(ArticleListPage) && e.Parameter is ArticleListPageParams parameters)
                    {
                        tag = parameters.FeedId;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    tag = item.Key;
                }

                NavView.SelectedItem = NavView.MenuItems
                    .OfType<NavigationViewItem>()
                    .First(n => n.Tag.Equals(tag));

                NavView.Header =
                    ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
            }
        }
    }
}
