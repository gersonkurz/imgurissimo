using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imgurissimo
{
    public class imageInfo 
    {
        public Uri Picture { get; private set; }
        public string Title { get; private set; }
        public readonly string Filename;

        public imageInfo(string title, string uri)
        {
            Filename = uri;
            Picture = new Uri(uri);
            Title = title;
        }
    }
}
