using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace idib_import
{
    
    public class DubbersHelper
    {
        private IdibData idibData;
        private IBrowsingContext browsingContext;
        private IHtmlParser htmlParser;

        public DubbersHelper(IdibData idibData)
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

                var source = filename.Replace("\\", "/");;
                if (source.Contains("doppiaggio"))
                {
                    source = source.Substring(source.IndexOf("doppiaggio") + "doppiaggio".Length + 1);
                }
                var dubber = new Dubber() {source = source};

                var dubberRef = document.QuerySelector("div[align]");

                ExtractDubberName(dubberRef, ref dubber);
                ExtractDubberPhoto(dubberRef, ref dubber);
                ExtractDubberAudio(document.QuerySelectorAll("table"), ref dubber);

                idibData.dubbers.Add(dubber);
            }            
        }

        private void ExtractDubberName(IElement dubberRef, ref Dubber dubber)
        {
            var elem = dubberRef.QuerySelector("font[color='#003300']");
            if (elem == null)
                return;
            
            dubber.name = Utils.Cleanup(elem.TextContent);

            if (dubber.name.Contains("\""))
            {
                var start = dubber.name.IndexOf("\"");
                var stop = dubber.name.LastIndexOf("\"");
                if (start != -1 && stop != -1)
                {
                    dubber.nickname = dubber.name;
                    dubber.name = Utils.Cleanup(dubber.nickname.Remove(start, stop - start + 1));
                }

            }

            if (dubber.name.Contains("("))
            {
                var start = dubber.name.IndexOf("(");
                var stop = dubber.name.LastIndexOf(")");
                if (start != -1 && stop != -1)
                {
                    dubber.altname = dubber.name.Substring(start + 1,stop - start - 1);
                    dubber.name = Utils.Cleanup(dubber.name.Remove(start, stop - start + 1));
                }
            }
        }

        private void ExtractDubberPhoto(IElement dubberRef, ref Dubber dubber)
        {
            var photo = dubberRef.QuerySelectorAll("img");
            if (photo.Length < 2 || photo[1].Attributes.GetNamedItem("src") == null)
                return;

            var parentPath = Utils.ParentURL(dubber.source);

            dubber.photo = new ImageRef();
            dubber.photo.name = Utils.CombineURL(parentPath, photo[1].Attributes.GetNamedItem("src").Value);
            dubber.photo.description = photo[1].Attributes.GetNamedItem("alt") != null ? photo[1].Attributes.GetNamedItem("alt").Value : null;
        }

        private void ExtractDubberAudio(IHtmlCollection<IElement> tablesRef, ref Dubber dubber)
        {
            if (tablesRef.Length < 3 || tablesRef[2].QuerySelector("a") == null)
                return;
            dubber.audio = new AudioRef();
            dubber.audio.description = Utils.Cleanup(tablesRef[2].TextContent.Replace("File audio:", null), false);
            dubber.audio.name = tablesRef[2].QuerySelector("a").Attributes.GetNamedItem("href").Value.Replace("../", null);
        }

    }
}