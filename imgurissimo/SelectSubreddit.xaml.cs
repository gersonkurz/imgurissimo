using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace imgurissimo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectSubreddit : Page
    {
        public ObservableCollection<SubredditItemTemplate> Subreddits = new ObservableCollection<SubredditItemTemplate>();

        public SelectSubreddit()
        {
            this.InitializeComponent();
            this.Loaded += SelectSubreddit_Loaded;
        }

        void SelectSubreddit_Loaded(object sender, RoutedEventArgs e)
        {
            Subreddits.Add(new SubredditItemTemplate("funny"));
            Subreddits.Add(new SubredditItemTemplate("earthporn"));
            this.itemListView.ItemsSource = Subreddits;
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            string subreddit = b.Content as string;
            this.Frame.Navigate(typeof(MainPage), subreddit);
        }
    }
}
