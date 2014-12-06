using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imgurissimo
{
    public class SubredditItemTemplate
    {
        public string Name { get; private set; }

        public SubredditItemTemplate(string name)
        {
            Name = name;
        }
    }
}
