using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage.Streams;
using System.Diagnostics;
using Windows.Storage;
using Windows.UI.Popups;
using System.IO;

namespace imgurissimo
{
    class imgurAPI
    {
        private string ClientID;
        private string ClientSecret;
        private readonly string BaseURL = "https://api.imgur.com/3/";
        private readonly HttpClient Client = new HttpClient();

        public imgurAPI()
        {
        }

        public async Task<string> GetSubreddit(string subreddit, string sort = "time", int page = 1 )
        {
            return await Get("/gallery/r/" + subreddit + "/" + sort + "/" + page.ToString());
        }

        public async Task<imageInfo> GetImage(string id)
        {
            string response = await Get("/gallery/r/image/" + id);

            JsonObject root = JsonObject.Parse(response);
            if(root.GetNamedBoolean("success"))
            {
                JsonObject data = root.GetNamedObject("data");
                return new imageInfo(
                    data.GetNamedString("title"),
                    data.GetNamedString("link"));
            }
            else
            {
                throw new Exception(
                    string.Format("ERROR: unable to query image {0}: {1}", 
                        id,
                        response));
            }
        }
        
        /* You'll need to create a file called keys.imgur in your documents library with two lines
         * somewhat like this:
         * 
         * client_id: 000000000000000
         * client_secret: 0000000000000000000000000000000000000000
         * 
         * */
        private async Task<IList<string>> ReadLicenseFile()
        {
            var folder = Windows.Storage.KnownFolders.DocumentsLibrary;
            var file = await folder.GetFileAsync("keys.imgur");
            return await FileIO.ReadLinesAsync(file);
        }

        public async Task<bool> Connect()
        {
            Debug.Assert(string.IsNullOrEmpty(ClientID));

            foreach (string line in await ReadLicenseFile())
            {
                if(line.StartsWith("client_id: "))
                {
                    ClientID = line.Split(':')[1].Trim();
                }
                else if(line.StartsWith("client_secret:"))
                {
                    ClientSecret = line.Split(':')[1].Trim();
                }
            }

            Client.DefaultRequestHeaders.Add("user-agent", "imgurissimo/0.1");
            Client.DefaultRequestHeaders.Add("Authorization", string.Format("Client-ID {0}", ClientID));

            string responseText = await Get("credits");
            Debug.WriteLine("Credits() returns {0}", responseText);

            JsonObject root = JsonObject.Parse(responseText);
            int status = (int)root.GetNamedNumber("status");
            return root.GetNamedBoolean("success");
        }

        public async Task<bool> SaveAsFile(string link, string subreddit)
        {
            int k = link.LastIndexOf("/");
            Debug.Assert(k >= 0);
            string filename = link.Substring(k + 1);

            HttpResponseMessage response = await Client.GetAsync(link);

            // Check that response was successful or throw exception
            response.EnsureSuccessStatusCode();

            // Read response asynchronously and save to file
            using(var inputStream = await response.Content.ReadAsStreamAsync())
            {
                var one = await KnownFolders.PicturesLibrary.CreateFolderAsync("r", CreationCollisionOption.OpenIfExists);
                var two = await one.CreateFolderAsync(subreddit, CreationCollisionOption.OpenIfExists);
                var storageFile = await two.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                using (Stream outputStream = await storageFile.OpenStreamForWriteAsync())
                {
                    await inputStream.CopyToAsync(outputStream);
                }

            }
            return true;
        }

        private async Task<string> Get(string request)
        {
            string RequestURL = BaseURL + request;
            Debug.WriteLine("HTTP GET {0}", request);
            HttpResponseMessage response = await Client.GetAsync(RequestURL);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
