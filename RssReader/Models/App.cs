namespace RssReader.Models
{
    public class Application
    {
        public string Id { get; set; }

        public string BaseUrl { get; set; } = "https://venyo.cn/rss";

        /// <summary>
        /// true means use WebView
        /// false means use system web browser
        /// </summary>
        public bool UseWebView { get; set; }
    }
}
