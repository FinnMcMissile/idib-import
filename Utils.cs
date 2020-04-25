using System.Globalization;
using System.Text.RegularExpressions;

namespace idib_import
{
    public static class Utils
    {
        public static string Cleanup(string input, bool titleCase = true)
        {
            var output = input.Replace("\n"," ").Replace("\t"," ").TrimEnd().TrimStart();
            output = Regex.Replace(output, " {2,}", " ");
            TextInfo ti = new CultureInfo("it-IT").TextInfo;
            if (titleCase)
                output = ti.ToTitleCase(ti.ToLower(output));
            return output;
        }

        public static string Unquote(string input)
        {
            if  (
                    (input.StartsWith("\"") && input.EndsWith("\"")) ||
                    (input.StartsWith("'") && input.EndsWith("'"))
                )
            return input.Substring(1, input.Length - 2);

            return input;
        }

        public static string ParentURL(string url)
        {
            int p = url.Replace('\\','/').LastIndexOf('/');
            if (p == -1)
                return string.Empty;
            return url.Substring(0, p);
        }

        public static string CombineURL(string parent, string child)
        {
            if (parent == string.Empty)
                return child;
                
            if (parent.Replace('\\','/').EndsWith('/'))
                return parent + child;

            return parent + '/' + child;
        }
    }
}