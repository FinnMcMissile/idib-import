using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Html.Parser;

namespace idib_import
{
    public class IndexHelper
    {
        private IdibData idibData;
        private IBrowsingContext browsingContext;
        private IHtmlParser htmlParser;

        public IndexHelper(IdibData idibData)
        {
            this.idibData = idibData;
            browsingContext = BrowsingContext.New(Configuration.Default);
            htmlParser = browsingContext.GetService<IHtmlParser>();
        }

        public void Add(string filename)
        {
            using (var streamReader = new StreamReader(filename, Encoding.GetEncoding(1252)))
            {
                var document = htmlParser.ParseDocument(streamReader.ReadToEnd());

                foreach (var indexRef in document.QuerySelectorAll("a"))
                {
                    var href = indexRef.Attributes.GetNamedItem("href");
                    if (href == null)
                        continue;

                    if (href.Value.StartsWith("voci/"))
                    {
                        if (idibData.dubbersIndex.ContainsKey(href.Value))
                        {
                            // @@TODO gestire multipli nomi per stesso elemento
                            // string existingValue;
                            // idibData.dubbersIndex.TryGetValue(href.Value, out existingValue);
                            // Console.WriteLine($"{filename}: multiple dubbers found: {href.Value} - (new){Utils.Cleanup(indexRef.TextContent)} - (old){existingValue}");
                        }
                        else
                        {
                            idibData.dubbersIndex.Add(href.Value, Utils.Cleanup(indexRef.TextContent));
                            // Console.WriteLine(href.Value + " - " + Utils.Cleanup(indexRef.TextContent));
                        }
                    }
                    if (href.Value.StartsWith("film/") || href.Value.StartsWith("film1/"))
                    {
                        if (idibData.moviesIndex.ContainsKey(href.Value))
                        {
                            // @@TODO gestire multipli nomi per stesso elemento
                            // string existingValue;
                            // idibData.moviesIndex.TryGetValue(href.Value, out existingValue);
                            // Console.WriteLine($"{filename}: multiple movies found: {href.Value} - (new){Utils.Cleanup(indexRef.TextContent)} - (old){existingValue}");
                        }
                        else
                        {
                            var title = Utils.Cleanup(indexRef.TextContent);
                            if (title.EndsWith(")"))
                            {
                                var start = title.LastIndexOf("(");
                                var stop = title.LastIndexOf(")");
                                if (start != -1 && stop != -1)
                                {
                                    // sometimes at the end there is the year of the movie (i.e.: Casino Royale (2006))
                                    var article = title.Substring(start + 1, stop - start - 1);
                                    var regex = new Regex("^[0-9]+$");
                                    if (!regex.IsMatch(article))
                                        title = article + " " + title.Remove(start, stop - start + 1).Trim();                                
                                }
                            }
                            idibData.moviesIndex.Add(href.Value, title);
                        }
                    }
                }
            }            
        }
    }
}