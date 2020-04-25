using System.Collections.Generic;

namespace idib_import
{
    public class ImageRef
    {
        public string name {get;set;}
        public string description {get;set;}
    }

    public class Gallery : List<ImageRef> {}
}