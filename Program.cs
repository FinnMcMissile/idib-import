﻿using System;
using System.IO;
using Newtonsoft.Json;

namespace idib_import
{
    class Options
    {
        public bool writeLog = false;
        public bool buildDictionary = false;
        public string dubbersPathname = string.Empty;
        public string moviesPathname = string.Empty; 
        public string outputPathname = string.Empty;
        public string indexPathname = string.Empty;
        public bool verbose = false;
        public string tmdbPathname = string.Empty;
    }

    class Program
    {
        static IdibData idibData = new IdibData();
        static AdditionalData additionalData = new AdditionalData();
        static Options options = new Options();

        static void dubbersScan(string pathName)
        {
            if (pathName == "")
                return;

            var dubbers = new DubbersHelper(idibData);

            FileAttributes attr = File.GetAttributes(pathName);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                foreach (var file in Directory.GetFiles(pathName,"*.htm"))
                {
                    dubbers.Add(file);
                }
            }
            else
            {
                dubbers.Add(pathName);
            }
        }
        
        static void moviesScan(string pathName, bool verbose)
        {
            if (pathName == "")
                return;
                
            var movies = new MoviesHelper(idibData);
            FileAttributes attr = File.GetAttributes(pathName);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                foreach (var file in Directory.GetFiles(pathName,"*.htm"))
                {
                    movies.Add(file, verbose);
                }
            }
            else
            {
                movies.Add(pathName, verbose);
            }
        }

        static void indexScan(string pathName)
        {
            if (pathName == "")
                return;
                
            var index = new IndexHelper(idibData);
            FileAttributes attr = File.GetAttributes(pathName);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                foreach (var file in Directory.GetFiles(pathName,"*.htm"))
                {
                    index.Add(file);
                }
            }
            else
            {
                index.Add(pathName);
            }
        }

        static void tmdbScan(string pathName, bool verbose) 
        {
            foreach (var movie in idibData.movies)
            {
                string message = string.Empty;
                int tmdbID = 0;

                if  (
                        TMDBHelper.search(movie, movie.originalTitle, "en-US", out tmdbID, out message) ||
                        TMDBHelper.search(movie, movie.italianTitle, "it-IT", out tmdbID, out message) ||
                        TMDBHelper.search(movie, movie.indexTitle, "it-IT", out tmdbID, out message) ||
                        TMDBHelper.search(movie, movie.originalTitle, "en-US", out tmdbID, out message, true) ||
                        TMDBHelper.search(movie, movie.italianTitle, "it-IT", out tmdbID, out message, true) ||
                        TMDBHelper.search(movie, movie.indexTitle, "it-IT", out tmdbID, out message, true)
                    )
                {
                    if (message != string.Empty && verbose)
                        Console.WriteLine($"{movie.originalTitle ?? movie.italianTitle}({movie.year}) - {tmdbID} ({message})");
                    additionalData.movies.Add(new Movie{
                        source = movie.source,
                        tmdbID = tmdbID
                    });
                }
                else
                {
                    if (verbose)
                        Console.WriteLine($"{movie.originalTitle}({movie.year}) - {message}");
                }
            }

            var stream = new StreamWriter(pathName); 
            stream.Write(
                JsonConvert.SerializeObject(
                    additionalData, 
                    Formatting.Indented,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore
                })
            );
            stream.Close();
        }

        static void writeOutput()
        {
            TextWriter stream = new StreamWriter(options.outputPathname); 
            stream.Write(
                JsonConvert.SerializeObject(
                    idibData, 
                    Formatting.Indented,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore
                })
            );
            stream.Close();

        }

        static bool readOptions(string[] args)
        {
            for (int a = 0; a < args.Length; a++ )
            {
                if (args[a] == "-log")
                    options.writeLog = true;
                else if (args[a] == "-dictionary")
                    options.buildDictionary = true;
                else if (args[a] == "-verbose")
                    options.verbose = true;
                else if (args[a] == "-output")
                    options.outputPathname = args[++a];
                else if (args[a] == "-dubbers")
                    options.dubbersPathname = args[++a];
                else if (args[a] == "-movies")
                    options.moviesPathname = args[++a];
                else if (args[a] == "-index")
                    options.indexPathname = args[++a];
                else if (args[a] == "-tmdb")
                    options.tmdbPathname = args[++a];
                else
                {
                    Console.WriteLine("Bad parameters");
                    return false;                     
                }
            }
            return true;
        }

        static int Main(string[] args)
        {
            if (!readOptions(args))
                return -1;

            if (options.indexPathname != string.Empty) indexScan(options.indexPathname);

            if (options.dubbersPathname != string.Empty) dubbersScan(options.dubbersPathname);
            if (options.moviesPathname != string.Empty) moviesScan(options.moviesPathname, options.verbose);

            if (options.buildDictionary) IdibDataHelper.BuildDictionary(idibData);

            if (options.writeLog) Console.WriteLine(IdibDataHelper.check(idibData));

            if (options.outputPathname != string.Empty) writeOutput();

            if (options.tmdbPathname != string.Empty) tmdbScan(options.tmdbPathname, options.writeLog);

            return 0;
        }
    }
}
