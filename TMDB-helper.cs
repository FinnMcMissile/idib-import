using System.Net.Http;
using Newtonsoft.Json;
using System;

namespace idib_import
{
    public class TMDBResult
    {
        public int id { get; set;}
        public string original_title { get; set;}
        public string release_date { get; set;}
    }

    public class TMDBResponse
    {
        public int page { get; set;}
        public TMDBResult[] results;
    }

    public class TMDBHelper
    {
        private static string API_KEY = "6648d088a16dd0ab9f95e42b6f7aa5d1";

        static public void getInfo(string tmdbID)
        {
            using (HttpClient client = new HttpClient())
            {
                var path = $"https://api.themoviedb.org/3/movie/{tmdbID}/translations?api_key={API_KEY}";
                HttpResponseMessage response = client.GetAsync(path).Result;
                if (response.IsSuccessStatusCode)
                {
                    var translations = response.Content.ReadAsStringAsync().Result;
                } 
            }

        }

        static public void search(Movie movie)
        {
            using (HttpClient client = new HttpClient())
            {
                var path = $"https://api.themoviedb.org/3/search/movie?api_key={API_KEY}&query={movie.originalTitle}&primary_release_year={movie.year}";
                // var path = $"https://api.themoviedb.org/3/search/movie?api_key={API_KEY}&query={movie.originalTitle}";
                HttpResponseMessage response = client.GetAsync(path).Result;
                if (response.IsSuccessStatusCode)
                {
                    var jsonMatches = response.Content.ReadAsStringAsync().Result;
                    var matches = JsonConvert.DeserializeObject<TMDBResponse>(jsonMatches); 
                    if (
                            matches.results.Length > 0 &&
                            matches.results[0].original_title == movie.originalTitle &&
                            matches.results[0].release_date.Substring(0, 4) == movie.year
                        )
                    {
                        Console.WriteLine($"{movie.originalTitle} - {matches.results[0].id}");
                    }
                    if ( matches.results.Length == 0)
                    {
                        Console.WriteLine($"{movie.originalTitle} - no match found");
                    }
                    if (
                            matches.results.Length > 0 &&
                            matches.results[0].original_title != movie.originalTitle
                        )
                    {
                        Console.WriteLine($"{movie.originalTitle} - matched {matches.results[0].original_title} ({matches.results[0].release_date})");
                    }
                } 
            }

        }
    }
}