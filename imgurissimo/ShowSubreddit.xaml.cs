using System;
using System.Collections.Generic;
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.System;
using System.Diagnostics;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using System.Collections.ObjectModel;
using Windows.UI.Popups;
using Windows.Data.Json;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace imgurissimo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShowSubredditPage : Page
    {
        public ObservableCollection<imageInfo> ListOfPictures = new ObservableCollection<imageInfo>();
        private string CurrentSubreddit;
        private int CurrentPage = 0;
        private bool HasReachedEndOfTheLine = false;
        private imgurAPI Client;

        public ShowSubredditPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            await SwitchToSubreddit(e.Parameter as string);
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            MyFlipView.ItemsSource = ListOfPictures;
        }

        private T FindFirstElementInVisualTree<T>(DependencyObject parentElement) where T : DependencyObject
        {
            if (parentElement != null)
            {
                var count = VisualTreeHelper.GetChildrenCount(parentElement);
                if (count == 0)
                    return null;

                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(parentElement, i);

                    if (child != null && child is T)
                        return (T)child;
                    else
                    {
                        var result = FindFirstElementInVisualTree<T>(child);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }
            return null;
        }

        private void UpdateStatus()
        {
            imageInfo info = MyFlipView.SelectedItem as imageInfo;
            double percent = 100.0 * MyFlipView.SelectedIndex / ListOfPictures.Count;
            MyInfoText.Text = string.Format("Image {0:000} of {1:000} ({2:00.00}%)\n{3}",
                MyFlipView.SelectedIndex + 1,
                ListOfPictures.Count,
                percent,
                info.Title);
        }

        private async void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyFlipView.SelectedIndex < 0)
                return;
            UpdateStatus();

            Debug.Assert(sender == MyFlipView);
            var flipViewItem = MyFlipView.ContainerFromIndex(MyFlipView.SelectedIndex);
            ResizeImageToFit(FindFirstElementInVisualTree<ScrollViewer>(flipViewItem));

            if(!HasReachedEndOfTheLine)
            {
                if( (MyFlipView.SelectedIndex > 1) && 
                    (MyFlipView.SelectedIndex == ListOfPictures.Count-1))
                {
                    Debug.WriteLine("need to load more...");
                    if(!await LoadMore(CurrentSubreddit))
                    {
                        Debug.WriteLine("SORRY: END OF LINE REACHED");
                        HasReachedEndOfTheLine = true;
                    }
                }
            }
        }

        private void DelayedResizeImageToFit(ScrollViewer sv)
        {
            var image = FindFirstElementInVisualTree<Image>(sv);
            if (image == null)
                return;

            var Scale = Window.Current.Bounds;

            double factor = 1.0;
            if (Scale.Width > Scale.Height)
            {
                factor = Scale.Height / image.ActualHeight;
            }
            else
            {
                factor = Scale.Width / image.ActualWidth;
            }
            if ((image.ActualHeight > 0) && (image.ActualHeight > 0))
            {
                float f = (float)factor;
                if (f != sv.ZoomFactor)
                {
                    sv.ChangeView(1.0f, 1.0f, f, true);
                }
            }
        }

        private void ResizeImageToFit(ScrollViewer sv)
        {
            if (sv == null)
                return;

            var period = TimeSpan.FromMilliseconds(10);
            Windows.System.Threading.ThreadPoolTimer.CreateTimer(async (source) =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    DelayedResizeImageToFit(sv);
                });
            }, period);
        }

        private void ScrollViewer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ResizeImageToFit(sender as ScrollViewer);
        }

        private async Task<bool> LoadMore(string subreddit)
        {
            MyFlipView.Opacity = 0.5;
            bool success = false;
            string response = await Client.GetSubreddit(subreddit, "time", CurrentPage+1);
            Debug.WriteLine(response);
            JsonObject root = JsonObject.Parse(response);
            if (root.GetNamedBoolean("success"))
            {
                JsonArray array = root.GetNamedArray("data");
                foreach (JsonValue value in array)
                {
                    JsonObject item = value.GetObject();
                    string link = item.GetNamedString("link");
                    string title = item.GetNamedString("title", "");
                    ListOfPictures.Add(new imageInfo(title, link));
                }
                CurrentPage += 1;
                UpdateStatus();
                if(CurrentPage == 0)
                {
                    CurrentSubreddit = subreddit;
                    ChangeButton.Content = string.Format("/r/{0}", subreddit);
                }
                success = true;
            }
            MyFlipView.Opacity = 1.0;
            return success;
        }

        public async Task<bool> SwitchToSubreddit(string subreddit)
        {
            MyFlipView.Opacity = 0.5;
            ListOfPictures.Clear();
            CurrentPage = -1;
            HasReachedEndOfTheLine = false;

            if(Client == null)
            {
                Client = new imgurAPI();
                if( !await Client.Connect() )
                {
                    var messageDialog = new MessageDialog("Sorry, you don't have a valid imgur key-set", "Oh my!");
                    await messageDialog.ShowAsync();
                    MyFlipView.Opacity = 1.0;
                    return false;
                }
            }
            return await LoadMore(subreddit);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MyFlipView.Opacity = 0.5;
            imageInfo info = MyFlipView.SelectedItem as imageInfo;
            await Client.SaveAsFile(info.Filename, CurrentSubreddit);
            MyFlipView.Opacity = 1.0;
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SelectSubreddit));
        }
    }
}
