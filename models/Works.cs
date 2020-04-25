using System.Collections.Generic;

namespace idib_import
{
    public class MovieRef
    {
        public string title {get; set;}
        public string source {get; set;}
        public string poster {get; set;}
    }
    

    public class Work
    {
        public MovieRef movie {get; set;}
        public string character {get; set;}
        public string actor {get; set;}
    }

    public class Works : List<Work> {}
}