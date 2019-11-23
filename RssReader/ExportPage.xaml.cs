using OPMLCore.NET;
using Rss.Common.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
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
    public sealed partial class ExportPage : Page
    {
        private static readonly string _fileName = "resader.opml";

        private StorageFolder folder;

        public ExportPage()
        {
            this.InitializeComponent();
        }

        private async void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            this.folder = await picker.PickSingleFolderAsync();
            if (this.folder == null)
            {
                return;
            }

            this.OpmlPath.Text = this.folder.Path;
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var feeds = Helper.Request<List<Feed>>("/rss/feeds", "GET");
            if (feeds == null || !feeds.Any())
            {
                return;
            }

            try
            {
                var opml = new Opml();
                opml.Encoding = "UTF-8";
                opml.Version = "2.0";
                var head = new Head();
                head.Title = _fileName;
                head.DateCreated = DateTime.Now;
                head.DateModified = DateTime.Now;
                opml.Head = head;

                Body body = new Body();
                feeds.ForEach(feed =>
                {
                    body.Outlines.Add(new Outline
                    {
                        Text = feed.Title,
                        Title = feed.Title,
                        Created = DateTime.Now,
                        HTMLUrl = feed.Url,
                        Type = "rss",
                        XMLUrl = feed.Url
                    });
                });
                opml.Body = body;

                var files = await this.folder.GetFilesAsync();
                StorageFile file;
                if (!files.Any(f => f.Name == _fileName))
                {
                    file = await this.folder.CreateFileAsync(_fileName);
                }
                else
                {
                    file = await this.folder.GetFileAsync(_fileName);
                }
                using(var writer = new StreamWriter(await file.OpenStreamForWriteAsync()))
                {
                    writer.Write(opml.ToString());
                }

                Helper.ShowMessageDialog("Tip", "Export success");
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.System.Launcher.LaunchFolderAsync(this.folder);
        }
    }
}
