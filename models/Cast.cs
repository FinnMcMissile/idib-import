using System.Collections.Generic;

namespace idib_import
{
    public class DubberRef
    {
        public string name {get; set;}
        public string source {get; set;}
        public string photo {get; set;}
        public string creditedAs {get; set;}
    }
    
    public class CastMember
    {
        public string character {get; set;}
        public string actor {get; set;}
        public DubberRef dubber {get; set;}
    }

    public class Cast : List<CastMember> {}
}