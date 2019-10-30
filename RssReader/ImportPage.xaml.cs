using OPMLCore.NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class ImportPage : Page
    {
        private StorageFile file;

        public ImportPage()
        {
            this.InitializeComponent();
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.file == null)
            {
                return;
            }

            try
            {
                using (var stream = await this.file.OpenStreamForReadAsync())
                {
                    var xml = new XmlDocument();
                    xml.Load(stream);
                    var opml = new Opml(xml);
                    var feeds = new List<string>();
                    opml.Body?.Outlines.ForEach(o => feeds.AddRange(this.GetFeeds(o)));
                    if (!string.IsNullOrWhiteSpace(Helper.Request<string>("/rss/addfeeds", "POST", feeds)))
                    {
                        Helper.ShowMessageDialog("Tip", "Import success");
                    }
                    else
                    {
                        Helper.ShowMessageDialog("Tip", "Failed to import");
                    }
                }
            }
            catch(Exception ex)
            {
                Helper.LogException(ex);
            }
        }

        private List<string> GetFeeds(Outline outline)
        {
            var feeds = new List<string>();
            if (outline.Outlines != null && outline.Outlines.Any())
            {
                outline.Outlines.ForEach(o => feeds.AddRange(this.GetFeeds(o)));
            }

            if(!string.IsNullOrWhiteSpace(outline.XMLUrl))
            {
                feeds.Add(outline.XMLUrl);
            }
            return feeds;
        }

        private async void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".opml");
            picker.FileTypeFilter.Add("*");

            this.file = await picker.PickSingleFileAsync();
            if (this.file == null)
            {
                return;
            }

            this.OpmlPath.Text = this.file.Path;
        }
    }
}
