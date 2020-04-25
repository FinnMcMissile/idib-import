using System.Collections.Generic;

namespace idib_import
{
    public class Movie
    {
        public string source {get; set;}
        public string originalTitle {get; set;}
        public string italianTitle {get; set;}
        public string indexTitle {get; set;}
        public string director {get; set;}
        public string year {get; set;}
        public string country {get; set;}
        public ImageRef poster {get; set;}
        public Gallery gallery {get; set;}
        public Cast cast {get; set;}
    }
}