using System.Net.Http;
using Newtonsoft.Json;
using System;

namespace idib_import
{
    public class TMDBResult
    {
        public int id { get; set;}
        public string original_title { get; set;}
        public string title { get; set;}
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

        static public bool search(Movie movie, string query, string language, out int id, out string message, bool fuzzyMatches = false)
        {
            if (query == null)
            {
                message = $"null query";
                id = 0;
                return false;
            }

            using (HttpClient client = new HttpClient())
            {
                var path = $"https://api.themoviedb.org/3/search/movie?api_key={API_KEY}&language={language}&query={Uri.EscapeUriString(query)}&primary_release_year={movie.year}";
                HttpResponseMessage response = client.GetAsync(path).Result;
                if (!response.IsSuccessStatusCode)
                {
                    message = $"Error in query response: {response.StatusCode}";
                    id = 0;
                    return false;
                }
                
                var matches = JsonConvert.DeserializeObject<TMDBResponse>(response.Content.ReadAsStringAsync().Result); 

                if (matches.results.Length == 0)
                {
                    message = $"0 matches found";
                    id = 0;
                    return false;
                }

                foreach (var result in matches.results)
                {
                    if  (
                            string.Equals(result.original_title, movie.originalTitle, StringComparison.InvariantCultureIgnoreCase) &&
                            result.release_date.Substring(0, 4) == movie.year
                        )
                    {
                        id = result.id;
                        message = string.Empty;
                        return true; // exact match
                    }
                }

                foreach (var result in matches.results)
                {
                    if  (
                            string.Equals(result.title, query, StringComparison.InvariantCultureIgnoreCase) &&
                            result.release_date.Substring(0, 4) == movie.year
                        )
                    {
                        id = result.id;
                        message = $"matched {result.original_title} ({result.release_date}) using {query} ({language})";
                        return true; // probable match
                    }
                }

                // the query returned some results, even if there is no precise match with the title
                if (fuzzyMatches)
                {
                    id = matches.results[0].id;
                    message = $"fuzzy matched {matches.results[0].original_title} ({matches.results[0].release_date}) using {query} ({language})";
                    return true; // probable match
                }

                message = $"no match found";
                id = 0;
                return false;
            }

        }
    }
}