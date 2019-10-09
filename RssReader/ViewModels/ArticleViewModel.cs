using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace RssReader.ViewModels
{
    public class ArticleViewModel : INotifyPropertyChanged
    {
        public string Id { get; set; }

        public string Url { get; set; }

        public string FeedId { get; set; }

        public string Title { get; set; }

        public string Summary { get; set; }

        public string Published { get; set; }

        public string Updated { get; set; }

        public string Created { get; set; }

        public string Keyword { get; set; }

        public string Content { get; set; }

        public string Contributors { get; set; }

        public string Authors { get; set; }

        public string Copyright { get; set; }

        private SymbolIcon icon;
        public SymbolIcon Icon
        {
            get => this.icon;
            set
            {
                this.icon = value;
                this.OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
