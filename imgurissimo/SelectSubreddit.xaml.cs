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
using Windows.Data.Json;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.Storage.Pickers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace imgurissimo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectSubreddit : Page
    {
        public ObservableCollection<SubredditItemTemplate> Subreddits = new ObservableCollection<SubredditItemTemplate>();
        private SubredditItemTemplate RootItem = null;
        private SubredditItemTemplate LastKnownGroup = null;

        public SelectSubreddit()
        {
            this.InitializeComponent();
            this.Loaded += SelectSubreddit_Loaded;
        }

        private async Task<JsonObject> ReadSubscriptionsFile(string filename)
        {
            var file = await StorageFile.GetFileFromPathAsync(filename);
            string text = await FileIO.ReadTextAsync(file);
            return JsonObject.Parse(text);
        }

        private bool DecodeGroupsRecursive(
            JsonObject subscriptions, 
            SubredditItemTemplate parent)
        {
            JsonArray subscriptionsArray = subscriptions.GetNamedArray("data");
            if(subscriptionsArray == null)
                return false;
            foreach (JsonValue value in subscriptionsArray)
            {
                JsonObject item = value.GetObject();
                string name = item.GetNamedString("name");
                string type = null;
                IJsonValue test;

                if( item.TryGetValue("type", out test))
                {
                    type = item.GetNamedString("type");
                }
                
                if( !string.IsNullOrEmpty(type) && 
                    string.Equals(type, "Group", StringComparison.OrdinalIgnoreCase))
                {
                    SubredditItemTemplate group = new SubredditItemTemplate(parent, name, true);
                    if(DecodeGroupsRecursive(item, group))
                    {
                        parent.Children.Add(group);
                    }
                }
                else
                {
                    parent.Children.Add(new SubredditItemTemplate(parent, name, false));
                }
            }
            return true;
        }

        private void SwitchToGroup(SubredditItemTemplate group)
        {
            Debug.Assert(LastKnownGroup != group);
            LastKnownGroup = group;

            Subreddits.Clear();
            if(group.Parent != null)
            {
                Subreddits.Add(group.Parent);
            }
            foreach(SubredditItemTemplate item in group.Children)
            {
                Subreddits.Add(item);
            }
        }

        async Task<bool> SwitchToSubscriptionsFile(string filename)
        {
            string errorText = "";
            try
            {
                JsonObject subscriptions = await ReadSubscriptionsFile(filename);

                SubredditItemTemplate item = new SubredditItemTemplate(null, "..", true);
                if( DecodeGroupsRecursive(subscriptions, item) )
                {
                    RootItem = item;
                    SwitchToGroup(RootItem);
                    return true;
                }
                return false;
            }
            catch(Exception e)
            {
                errorText = e.ToString();
            }
            var messageDialog = new MessageDialog(
                string.Format("Sorry, unable to read {0}: {1}", filename, errorText), "Error");
            await messageDialog.ShowAsync();
            return false;

        }

        async void SelectSubreddit_Loaded(object sender, RoutedEventArgs e)
        {
            this.itemListView.ItemsSource = Subreddits;

            string configFile = ApplicationData.Current.LocalSettings.Values["config-file"] as string;
            if (!string.IsNullOrEmpty(configFile))
            {
                if (await SwitchToSubscriptionsFile(configFile))
                    return;
            }
            
            await ChooseConfigurationFile();
        }
    
        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            var template = b.Tag as SubredditItemTemplate;
            if(template.IsDirectory)
            {
                SwitchToGroup(template);
            }
            else
            {
                this.Frame.Navigate(typeof(ShowSubredditPage), template.Name);
            }
        }

        private async Task<bool> ChooseConfigurationFile()
        {
            var p = new FileOpenPicker();
            p.FileTypeFilter.Add(".imgur");
            p.ViewMode = PickerViewMode.List;
            p.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            p.SettingsIdentifier = "FilePicker";

            var file = await p.PickSingleFileAsync();
            if (file != null)
            {
                if( await SwitchToSubscriptionsFile(file.Path) )
                {
                    ApplicationData.Current.LocalSettings.Values["config-file"] = file.Path;
                    return true;

                }
            }
            return false;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await ChooseConfigurationFile();

        }
    }
}

