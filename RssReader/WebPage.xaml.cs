﻿using RssReader.Models;
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
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RssReader
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WebPage : Page
    {
        public WebPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter != null && e.Parameter is WebPageParams parameters)
            {
                try
                {
                    this.WebView.Source = new Uri(parameters.Url);
                }
                catch(Exception ex)
                {
                    Helper.LogException(ex);
                }
            }
        }

        public bool GoBackWebView()
        {
            if (!this.WebView.CanGoBack)
            {
                return false;
            }
                
            this.WebView.GoBack();
            return true;
        }
    }
}
