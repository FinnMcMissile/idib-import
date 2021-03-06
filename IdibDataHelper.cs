using System;

namespace idib_import
{
    public class IdibDataHelper
    {
        public static string check(IdibData idibData)
        {
            var log = string.Empty;
            foreach (var dubber in idibData.dubbers)
            {
                if (dubber.name == null)
                    log+= $"Missing person name in {dubber.source}\n";
                
                if (dubber.indexName == null)
                    log+= $"Missing index person name in {dubber.source}\n";
            }
            foreach (var movie in idibData.movies)
            {
                if (movie.originalTitle == null && movie.italianTitle == null)
                    log+= $"Missing movie title in {movie.source}\n";

                if (movie.indexTitle == null)
                    log+= $"Missing movie index title in {movie.source}\n";
                
                foreach (var member in movie.cast)
                {
                    if (member.dubber.name != null && member.dubber.source == null)
                        log += $"Missing dubber link {movie.source} : {member.dubber.name}\n";
                }
            }

            return log;
        }

        // set the indexed name of the dubbers
        private static void DubbersIndexedName(IdibData idibData)
        {
            foreach (var dubber in idibData.dubbers)
            {
                string indexName;
                if (idibData.dubbersIndex.TryGetValue(dubber.source, out indexName))
                {
                    dubber.indexName = indexName;
                }
            }
        }
        
        // set the indexed name of the movies
        private static void MoviesIndexedName(IdibData idibData)
        {
            foreach (var movie in idibData.movies)
            {
                string indexTitle;
                if (idibData.moviesIndex.TryGetValue(movie.source, out indexTitle))
                {
                    movie.indexTitle = indexTitle;
                }
            }
        }

        private static Dubber FindDubberByName(IdibData idibData, string name)
        {
            // try to find with the exact name
            var dubber = idibData.dubbers.Find(dub => dub.name == name);
            if (dubber != null)
                return dubber;

            // find by some nickname or alternative

            return idibData.dubbers.Find(dub => 
                {
                    // e.g Stefano Satta Flores vs Stefano Satta-Flores
                    if (dub.name.Replace("-"," ") == name.Replace("-"," "))
                        return true;
                    if (dub.altname != null && dub.altname == name)
                        return true; 
                    if  (
                            dub.nickname != null &&
                            dub.nickname.Replace("'","").Replace("\"","") == name.Replace("'","").Replace("\"","")
                        )
                        return true;
                    return false;
                });
        } 

        // set the movies each dubber worked on
        private static void SetDubbersWorks(IdibData idibData)
        {
            foreach (var movie in idibData.movies)
            {
                foreach (var member in movie.cast)
                {
                    if (member.dubber == null || member.dubber.name == null)
                        continue;

                    var dubber = FindDubberByName(idibData, member.dubber.name);

                    if (dubber != null && dubber.name != member.dubber.name)
                    {
                        member.dubber.creditedAs = member.dubber.name;
                        member.dubber.name = dubber.name;
                    }

                    if (dubber == null)
                        continue;

                    member.dubber.photo = dubber.photo != null ? dubber.photo.name : null;
                    member.dubber.source = dubber.source;
                    if (dubber.works == null)
                        dubber.works = new Works();
                    dubber.works.Add(new Work()
                    {
                        movie = new MovieRef() {
                            title = !string.IsNullOrEmpty(movie.indexTitle) ? movie.indexTitle : 
                                    !string.IsNullOrEmpty(movie.italianTitle) ? movie.italianTitle :
                                    movie.originalTitle,
                            source = movie.source,
                            poster = movie.poster != null ? movie.poster.name : null
                        },
                        character = member.character,
                        actor = member.actor
                    });
                }
            }
        }

        // insert some markdown hrefs in dubbers data
        private static void InsertDubbersHref(IdibData idibData)
        {
            foreach (var dubber in idibData.dubbers)
            {
                if (dubber.audio == null)
                    continue;
                if (dubber.audio.description.Contains("\""))
                {
                    var start = dubber.audio.description.IndexOf("\"");
                    var stop = dubber.audio.description.LastIndexOf("\"");
                    if (start != -1 && stop != -1)
                    {
                        var movieTitle = dubber.audio.description.Substring(start + 1, stop - start - 1);
                        var movie = idibData.movies.Find( m => {
                            if (m.indexTitle != null && m.indexTitle.Equals(movieTitle, StringComparison.InvariantCultureIgnoreCase))
                                return true;
                            if (m.italianTitle != null && m.italianTitle.Equals(movieTitle, StringComparison.InvariantCultureIgnoreCase))
                                return true;
                            if (m.originalTitle != null && m.originalTitle.Equals(movieTitle, StringComparison.InvariantCultureIgnoreCase)) 
                                return true;
                            return false;
                        });
                        if (movie != null) 
                        {
                            dubber.audio.description = dubber.audio.description.
                                    Remove(start, stop - start + 1).
                                    Insert(start, $"[{movieTitle}](movie-page.html?movieSource={movie.source})");
                        }
                    }

                }
            }
        }

        public static void FindDubbersInAdditionalInfos(IdibData idibData)
        {
            foreach (var movie in idibData.movies)
            {
                foreach (var info in movie.additionalInfos)
                {
                    var dubber = FindDubberByName(idibData, info.content);
                    if (dubber != null)
                        info.content = $"[{dubber.name}](dubber-page.html?dubberSource={dubber.source})";
                    if (info.content.Contains(","))
                    {
                        var parts = info.content.Split(",");
                        info.content = string.Empty;
                        foreach (var part in parts)
                        {
                            if (info.content != string.Empty)   info.content += ", ";
                            var dub = FindDubberByName(idibData, Utils.Cleanup(part));
                            if (dub != null)
                            {
                                info.content += $"[{dub.name}](dubber-page.html?dubberSource={dub.source})";
                            }
                            else
                            {
                                info.content += part;
                            }
                        }
                    }
                }
            }
        }

        public static void BuildDictionary(IdibData idibData)
        {
            DubbersIndexedName(idibData);
            MoviesIndexedName(idibData);
            SetDubbersWorks(idibData);
            InsertDubbersHref(idibData);
            FindDubbersInAdditionalInfos(idibData);

        }
    }

}