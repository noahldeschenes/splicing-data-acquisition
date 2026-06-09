
//using Utils;

using System.Text.Json;
using System.Text.RegularExpressions;


namespace RecordSplicingResults
{
    /*
        Folder: date
        Files inside: JSON, images, settings, errors

        maybe add original arc time/power and give a difference? ask mark

    */



    class Program
    {

        static Dictionary<String, String> GetDictFromSplicerOutput(string splicerOutput)
        {
            // <summary> Takes the output of the splicer and formats it into a dict, for eventual
            // conversion into a JSON. </summary>

            // TODO: add conversions to ints/floats

            Dictionary<string, string> pairs = new();

            string pattern = @"(?<identifier>[^|=]+)=(?<result>[^|]*)";  // input form: IDENTIFIER1=RESULT1|IDENTIFIER2=RESULT2|...   

            foreach (Match match in Regex.Matches(splicerOutput, pattern))
            {
                string id = match.Groups["identifier"].Value;
                string result = match.Groups["result"].Value; 
                pairs[id] = result;
            }

            return pairs;
        }


        static string QuerySplicer(string queryType, string[] identifiers)
        {
            return "";
        }

        

    }
}

