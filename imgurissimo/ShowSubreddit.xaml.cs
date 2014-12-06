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
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<imgur.imageInfo> ListOfPictures = new ObservableCollection<imgur.imageInfo>();
        private string CurrentSubreddit = "funny";
        private int CurrentPage = 0;
        private imgurAPI Client;

        public MainPage()
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

        private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyFlipView.SelectedIndex < 0)
                return;
            imgur.imageInfo info = MyFlipView.SelectedItem as imgur.imageInfo;

            double percent = 100.0 * MyFlipView.SelectedIndex / ListOfPictures.Count;
            MyInfoText.Text = string.Format("Image {0:000} of {1:000} ({2:00.00}%)\n{3}",
                MyFlipView.SelectedIndex + 1,
                ListOfPictures.Count,
                percent,
                info.Title);

            Debug.Assert(sender == MyFlipView);
            var flipViewItem = MyFlipView.ContainerFromIndex(MyFlipView.SelectedIndex);
            ResizeImageToFit(FindFirstElementInVisualTree<ScrollViewer>(flipViewItem));
            MyAppBar.IsEnabled = true;
        }



        private async Task<bool> PhysicallyDeleteThisFile(string filename)
        {
            try
            {
                Debug.WriteLine("Physically delete {0}", filename);
                var storageFile = await StorageFile.GetFileFromPathAsync(filename);
                await storageFile.DeleteAsync();
                return true;
            }
            catch (Exception)
            {
                Debug.WriteLine("Sorry, unable to delete {0}", filename);
                return false;
            }
        }

        private void DelayedResizeImageToFit(ScrollViewer sv)
        {
            Debug.WriteLine("DelayedResizeImageToFit called");

            var image = FindFirstElementInVisualTree<Image>(sv);
            if (image == null)
            {
                Debug.WriteLine("DelayedResizeImageToFit aborts, because image is null");
                return;
            }

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

                Debug.WriteLine("image: {0:0000.00} x {1:0000.00}, Screen: {2:0000.00} x {3:0000.00}, Zoom:{4:0000.00}",
                    image.ActualWidth,
                    image.ActualHeight,
                    Scale.Width,
                    Scale.Height,
                    factor);

                float f = (float)factor;
                if (f != sv.ZoomFactor)
                {
                    sv.ChangeView(1.0f, 1.0f, f, true);
                }
            }
            else
            {
                Debug.WriteLine("DelayedResizeImageToFit does nothing, image size is not valid yet");

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

        public async Task<bool> SwitchToSubreddit(string subreddit)
        {
            MyFlipView.Opacity = 0.5;
            ListOfPictures.Clear();
            CurrentPage = 0;

            if(Client == null)
            {
                Client = new imgurAPI();
                if( !await Client.Connect() )
                {
                    var messageDialog = new MessageDialog("Sorry, you don't have a valid imgur key-set", "Oh my!");
                    await messageDialog.ShowAsync();
                    return false;
                }
            }

            
             
            string response = await Client.GetSubreddit(subreddit, "time", CurrentPage);
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
                    ListOfPictures.Add(new imgur.imageInfo(title, link));
                }
                CurrentSubreddit = subreddit;
            }
            else
            {
                Debug.WriteLine("imgur cannot read gallery: {0}", response);
            }
            MyFlipView.Opacity = 1.0;
            return true;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MyFlipView.Opacity = 0.5;
            imgur.imageInfo info = MyFlipView.SelectedItem as imgur.imageInfo;
            await Client.SaveAsFile(info.Filename);
            MyFlipView.Opacity = 1.0;
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SelectSubreddit));
        }
    }
}
