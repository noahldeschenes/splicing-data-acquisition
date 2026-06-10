
//using Utils;

using System.Text.Json;
using System.Text.RegularExpressions;
using Utils;


namespace RecordSplicingResults
{
    /*
        Folder: date
        Files inside: JSON, images, settings, errors

    */

    class Program
    {

        static Dictionary<string, object> ParseSplicerOutput(string splicerOutput)
        {
            // <summary> Takes the output of the splicer and formats it into a dict, for eventual
            // conversion into a JSON. </summary>

            Dictionary<string, object> pairs = new();

            string pattern = @"(?<identifier>[^|=]+)=(?<result>[^|]*)";  // input form: IDENTIFIER1=RESULT1|IDENTIFIER2=RESULT2|...   

            foreach (Match match in Regex.Matches(splicerOutput, pattern))
            {
                string id = match.Groups["identifier"].Value;
                string result = match.Groups["result"].Value; 

                
                // result can be a string, an int, or a float, so we auto-parse it for the sake of convenience/flexibility
                
                if (!int.TryParse(result, out int convertedResult) && !float.TryParse(result, out float convertedResult))
                {
                    string convertedResult = result;
                }

                pairs[id] = convertedResult;
            }

            return pairs;
        }


        static string QuerySplicer(string queryType, string[] identifiers)
        {
            
            string input = queryType;
            
            foreach (string id in identifiers)
            {
                input+=$"|{id}";
            }

            SplicerUtils.splicer.CommandAndRecieveText(input);
        }

        

    }
}

