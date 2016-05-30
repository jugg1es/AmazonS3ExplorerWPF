using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonS3ExplorerWPF
{
    public class AmazonUploadObject
    {
        public string LocalPath { get; set; }
        public string RemotePath { get; set; }
    }
    public class GetRequestDescription
    {
        public FolderTreeViewModel ItemToRefresh { get; set; }
        public bool AutoSelect { get; set; }
    }
    public class SourceDestinationData
    {
        public string Source { get; set; }
        public string Destination { get; set; }
    }
    public class CopyAmazonStructure
    {
        public string SourceParentPrefix { get; set; }
        public string SourcePrefix { get; set; }
        public string FolderPrefix { get; set; }
        public List<string> OriginalKeysToCopy { get; set; }
        public bool Cutting { get; set; }

    }
}
