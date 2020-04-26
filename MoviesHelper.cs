using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json;

namespace idib_import
{
    public class MoviesHelper
    {
        private IdibData idibData;
        private IBrowsingContext browsingContext;
        private IHtmlParser htmlParser;

        public MoviesHelper(IdibData idibData)
        {
            this.idibData = idibData;
            browsingContext = BrowsingContext.New(Configuration.Default);
            htmlParser = browsingContext.GetService<IHtmlParser>();
        }

        public void Add(string filename, bool verbose)
        {
            using (var streamReader = new StreamReader(filename, Encoding.GetEncoding(1252)))
            {
                var document = htmlParser.ParseDocument(streamReader.ReadToEnd());

                var source = filename.Replace("\\", "/");
                if (source.Contains("doppiaggio"))
                {
                    source = source.Substring(source.IndexOf("doppiaggio") + "doppiaggio".Length + 1);
                }
                var movie = new Movie() {source = source};

                ExtractMoviePosters(document.QuerySelector("body"), ref movie);
                foreach (var movieInfoRef in document.QuerySelectorAll("ul"))
                {
                    if  (
                            movieInfoRef.PreviousElementSibling != null &&
                            movieInfoRef.PreviousElementSibling.TextContent.Contains("ALCUNE NOTE SUL FILM", StringComparison.InvariantCultureIgnoreCase)
                        )
                        continue;
                    ExtractMovieInfo(movieInfoRef, ref movie);
                }
                foreach (var movieCastRef in document.QuerySelectorAll("div[align=center]"))
                {
                    ExtractMovieCast(movieCastRef, ref movie);
                }

                idibData.movies.Add(movie);

                if (verbose) {
                    Console.WriteLine(
                        JsonConvert.SerializeObject(
                        movie, 
                        Formatting.Indented,
                        new JsonSerializerSettings {
                            NullValueHandling = NullValueHandling.Ignore
                        })
                    );                  
                }
            }            
        }

        private void ExtractMoviePosters(IElement movieRef, ref Movie movie)
        {
            var poster = movieRef.QuerySelectorAll("img");
            // the first image is the page logo
            if (poster.Length < 2 || poster[1].Attributes.GetNamedItem("src") == null)
                return;

            var parentPath = Utils.ParentURL(movie.source);                

            movie.poster = new ImageRef();
            movie.poster.name = Utils.CombineURL(parentPath, poster[1].Attributes.GetNamedItem("src").Value);            
            movie.poster.description = poster[1].Attributes.GetNamedItem("alt") != null ? poster[1].Attributes.GetNamedItem("alt").Value : null;

            // all other images are extra poster gallery
            if (poster.Length > 2)
            {
                movie.gallery = new Gallery();
                for (int p = 2; p < poster.Length; p++ )
                {
                    if (poster[p].Attributes.GetNamedItem("src") == null)
                        continue;

                    movie.gallery.Add
                    (
                        new ImageRef() 
                        { 
                            name = Utils.CombineURL(parentPath, poster[p].Attributes.GetNamedItem("src").Value),
                            description = poster[p].Attributes.GetNamedItem("alt") != null ? poster[p].Attributes.GetNamedItem("alt").Value : null
                        }
                    );
                }        
            }
        }

        private bool ExtractInfo(IElement elem, string pattern, ref string result)
        {
            if (Regex.Match(elem.TextContent, pattern).Success)
            {
                var info =  Regex.Replace(elem.TextContent,pattern,"");
                result = Utils.Unquote(Utils.Cleanup(info));
                return true;
            }
            return false;
        }

        private void ExtractMovieInfo(IElement movieInfoRef, ref Movie movie)
        {
            var infos = movieInfoRef.QuerySelectorAll("li");
            var result = string.Empty;
            movie.additionalInfos = new AdditionalInfos();
            foreach (var elem in infos)
            {
                if (ExtractInfo(elem, "TITOLO.*ITALIANO*.:", ref result))          movie.italianTitle = result;
                else if (ExtractInfo(elem, "TITOLO.*ITALIANO*.:", ref result))     movie.italianTitle = result;
                else if (ExtractInfo(elem, "TITOLO.*ORIGINALE*.:", ref result))    movie.originalTitle = result;
                else if (ExtractInfo(elem, "REGIA*.:", ref result))                movie.director = result;
                else if (ExtractInfo(elem, "PRODUZIONE*.:", ref result))
                {
                    var regex = new Regex("^[0-9]+$");
                    if (regex.IsMatch(result.Substring(result.Length - 4)))
                    {
                        movie.year = result.Substring(result.Length - 4);
                        movie.country = result.Substring(0, result.Length - 4).Trim();
                    }
                    else
                    {
                        movie.additionalInfos.Add(new AdditionalInfo()
                        {
                            description = "Casa cinematografica",
                            content = result
                        });
                    }
                }
                else
                {
                    var info = elem.TextContent.Split(":");
                    if (info.Length == 2)
                    {
                        movie.additionalInfos.Add(new AdditionalInfo()
                        {
                            description = Utils.Cleanup(info[0]),
                            content = Utils.Cleanup(info[1])
                        });
                    }
                }
            }
        }

        private string ExtractCastInfo(string rawCastInfo)
        {
            var castInfo = Utils.Cleanup(rawCastInfo);
            castInfo = castInfo.Replace("(*)","").Replace("(?)","").Replace("??","").Trim();
            if (castInfo == string.Empty)
                return null;
            if (castInfo.StartsWith("--"))
                return null;
            return castInfo;
        }

        private string DubberCleanup(string rawDubber)
        {
            if (rawDubber == null)
                return null;
            var dubber = rawDubber;
            if (dubber.Contains("("))
            {
                var start = dubber.IndexOf("(");
                var stop = dubber.LastIndexOf(")");
                if (start != -1 && stop != -1)
                    dubber = dubber.Remove(start, stop - start + 1).Trim();
            }
            return dubber;
        }

        private bool IsVocalPlay(IHtmlCollection<IElement> line, int it)
        {
            return  line[1].TextContent.Contains("(voce)", StringComparison.InvariantCultureIgnoreCase) &&
                    line[1].TextContent.Contains("(canto)", StringComparison.InvariantCultureIgnoreCase)
                    ||
                    line[it].TextContent.Contains("(voce)", StringComparison.InvariantCultureIgnoreCase) &&
                    line[it].TextContent.Contains("(canto)", StringComparison.InvariantCultureIgnoreCase);
        }

// "actor": "Scott Weinger (Voce) Brad Kane (Canto)",
// "dubber": "Massimiliano Alto (Voce) Vincent Thoma (Canto)"

        private void ExtractActor(string actors, ref string singer, ref string voice)
        {
            var v = actors.IndexOf("(voce)",StringComparison.InvariantCultureIgnoreCase);
            var c = actors.IndexOf("(canto)",StringComparison.InvariantCultureIgnoreCase);
            if (v == -1 || c == -1)
            {
                singer = voice = actors.Replace("(voce)","",StringComparison.InvariantCultureIgnoreCase).Replace("(canto)","",StringComparison.InvariantCultureIgnoreCase).Trim();
            }
            else
            {
                voice = actors.Substring(0,v).Trim();
                singer = actors.Substring(v + "(voce)".Length, c - (v + "(voce)".Length)).Trim();
            }
        }

        private void AddVocalPlay(IHtmlCollection<IElement> line, int it, ref Movie movie)
        {
            string singingActor = string.Empty, voiceActor = string.Empty, singingDubber = string.Empty, voiceDubber = string.Empty;
            var character = ExtractCastInfo(line[0].TextContent);
            ExtractActor(ExtractCastInfo(line[1].TextContent), ref singingActor, ref voiceActor);
            ExtractActor(ExtractCastInfo(line[it].TextContent), ref singingDubber, ref voiceDubber);

            movie.cast.Add(
                new CastMember()
                {
                    character = character + " (Voce)",
                    actor = voiceActor,
                    dubber = new DubberRef() { name = voiceDubber }
                }
            );

            movie.cast.Add(
                new CastMember()
                {
                    character = character + " (Canto)",
                    actor = singingActor,
                    dubber = new DubberRef() { name = singingDubber }
                }
            ); 
        }

        private bool IsMultipleDubbers(IHtmlCollection<IElement> line, int it)
        {
            return line[it].Html().Contains("<br>");
        }

        private void AddMultipleDubbers(IHtmlCollection<IElement> line, int it, ref Movie movie)
        {
            line[it].InnerHtml = line[it].InnerHtml.Replace("<br>",",");
            var dubbers = line[it].TextContent.Split(",");

            foreach (var dubber in dubbers)
            {
                if (dubber.Contains("per gentile concessione"))
                    continue;
                movie.cast.Add(
                    new CastMember()
                    {
                        character = ExtractCastInfo(line[0].TextContent),
                        actor = ExtractCastInfo(line[1].TextContent),
                        dubber = new DubberRef() { name = DubberCleanup(ExtractCastInfo(dubber)) }
                    }
                ); 
            }
        }

        private void ExtractMovieCast(IElement movieCastRef, ref Movie movie)
        {
            var cast = movieCastRef.QuerySelectorAll("tr");
            if (cast.Length < 2)
                return;
            if (movie.cast == null)
                movie.cast = new Cast();
            for (int c = 1; c < cast.Length; c++)
            {
                var line = cast[c].QuerySelectorAll("td");
                var it = line.Length == 3 ? 2 : 3;

                if (IsVocalPlay(line, it))
                    AddVocalPlay(line, it, ref movie);
                else if (IsMultipleDubbers(line, it))
                    AddMultipleDubbers(line, it, ref movie);
                else
                {
                    movie.cast.Add(new CastMember() {
                        character = ExtractCastInfo(line[0].TextContent),
                        actor = ExtractCastInfo(line[1].TextContent),
                        dubber = new DubberRef() { name = DubberCleanup(ExtractCastInfo(line[it].TextContent)) }
                    });
                }
            }
        }
    }
}
