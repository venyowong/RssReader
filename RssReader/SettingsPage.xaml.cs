using RssReader.Daos;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            this.UseWebView.Text = Helper.App.UseWebView.ToString().ToLower();
            if (Helper.App.UseWebView)
            {
                this.UseWebView.SelectedIndex = 0;
            }
            else
            {
                this.UseWebView.SelectedIndex = 1;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appDao = new AppDao();
                appDao.DeleteApp();
                Helper.App.Id = this.AppId.Text;
                Helper.App.BaseUrl = this.BaseUrl.Text;
                Helper.App.UseWebView = this.UseWebView.SelectedIndex == 0;
                appDao.InsertApp(Helper.App);
                Helper.ReInit();
                Helper.ShowMessageDialog("Message", "Save ok.");
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
            }
        }
    }
}
