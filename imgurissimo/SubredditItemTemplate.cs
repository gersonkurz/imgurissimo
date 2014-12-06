using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace imgurissimo
{
    public class SubredditItemTemplate
    {
        public string Name { get; private set; }
        public readonly bool IsDirectory;        
        private static SolidColorBrush FolderColor = new SolidColorBrush(Colors.DarkBlue);
        private static SolidColorBrush FileColor = new SolidColorBrush(Colors.DarkGreen);

        public SolidColorBrush Background
        {
            get
            {
                if (IsDirectory)
                {
                    return FolderColor;
                }
                return FileColor;
            }
        }
        public SubredditItemTemplate Parent { get; private set; }
        public readonly List<SubredditItemTemplate> Children = new List<SubredditItemTemplate>();

        public SubredditItemTemplate(SubredditItemTemplate parent, string name, bool isDirectory)
        {
            Parent = parent;
            Name = name;
            IsDirectory = isDirectory;
        }
    }
}
