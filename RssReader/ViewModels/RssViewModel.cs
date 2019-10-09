using Rss.Common.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RssReader.ViewModels
{
    public class RssViewModel : INotifyPropertyChanged
    {
        private string title;
        public string Title
        {
            get => this.title;
            set
            {
                this.title = value;
                this.OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private List<ArticleViewModel> articles;

        public List<ArticleViewModel> Articles
        {
            get => this.articles;
            set
            {
                this.articles = value;
                this.OnPropertyChanged();
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
