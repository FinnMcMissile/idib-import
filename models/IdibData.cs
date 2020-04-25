using System.Collections.Generic;
using System.Runtime.Serialization;

namespace idib_import
{
    [DataContract]
    public class IdibData
    {
        [DataMember]
        public List<Dubber> dubbers = new List<Dubber>();
        [DataMember]
        public List<Movie> movies = new List<Movie>();

        public Dictionary<string, string> dubbersIndex {get; set;} = new Dictionary<string, string>();
        public Dictionary<string, string> moviesIndex = new Dictionary<string, string>();
    }
}