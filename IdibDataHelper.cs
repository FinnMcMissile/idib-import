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
                    if (member.dubber.source == null)
                        log += $"Missing dubber link {movie.source} : {member.dubber.name}\n";
                }
            }

            return log;
        }

        public static void BuildDictionary(IdibData idibData)
        {
            // set the indexed name of the dubbers
            foreach (var dubber in idibData.dubbers)
            {
                string indexName;
                if (idibData.dubbersIndex.TryGetValue(dubber.source, out indexName))
                {
                    dubber.indexName = indexName;
                }
            }

            // set the indexed name of the movies
            foreach (var movie in idibData.movies)
            {
                string indexTitle;
                if (idibData.moviesIndex.TryGetValue(movie.source, out indexTitle))
                {
                    movie.indexTitle = indexTitle;
                }
            }

            // set the movies each dubber worked on
            foreach (var movie in idibData.movies)
            {
                foreach (var member in movie.cast)
                {
                    if (member.dubber == null || member.dubber.name == null)
                        continue;

                    // try to find with the exact name
                    var dubber = idibData.dubbers.Find(dub => dub.name == member.dubber.name);
                    if (dubber == null)
                    {
                        // find by some nickname or alternative

                        dubber = idibData.dubbers.Find(dub => 
                        {
                            // e.g Stefano Satta Flores vs Stefano Satta-Flores
                            if (dub.name.Replace("-"," ") == member.dubber.name.Replace("-"," "))
                                return true;
                            if (dub.altname != null && dub.altname == member.dubber.name)
                                return true; 
                            if  (
                                    dub.nickname != null &&
                                    dub.nickname.Replace("'","").Replace("\"","") == member.dubber.name.Replace("'","").Replace("\"","")
                                )
                                return true;
                            return false;
                        });

                        if (dubber != null)
                        {
                            member.dubber.creditedAs = member.dubber.name;
                            member.dubber.name = dubber.name;
                        }
                    }

                    if (dubber != null)
                    {
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

        }
    }

}