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
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RssReader
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            this.AppId.Text = Helper.App.Id;
            this.BaseUrl.Text = Helper.App.BaseUrl;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            using(var context = new AppDbContext())
            {
                context.App.Remove(Helper.App);
                context.SaveChanges();
                Helper.App.Id = this.AppId.Text;
                Helper.App.BaseUrl = this.BaseUrl.Text;
                context.App.Add(Helper.App);
                context.SaveChanges();
                Helper.ReInit();
                Helper.ShowMessageDialog("Message", "Save ok.");
            }
        }
    }
}
