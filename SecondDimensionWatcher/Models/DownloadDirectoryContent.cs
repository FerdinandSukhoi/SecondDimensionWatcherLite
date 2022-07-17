using System.Collections.Generic;

namespace SecondDimensionWatcher.Models
{
    public class DownloadDirectoryContent : DownloadContent
    {
        public ICollection<DownloadContent> SubContents { get; set; }
    }
}