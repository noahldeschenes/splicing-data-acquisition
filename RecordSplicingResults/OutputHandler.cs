
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace RecordSplicingResults
{
    static class OutputHandler
    {
        
        public const int NUM_OF_MODES = 300; 
        public const string NAK = "\x15"; // ASCII code for NAK
        public static IUsbFsm100ServerClass splicer;
        

        private static object AutoParse(string result)
        {
            if (int.TryParse(result, out int resultAsInt)) return resultAsInt;
            else if (float.TryParse(result, out float resultAsFloat)) return resultAsFloat;
            else return result;
        }

        private static object? GetSpecificResultFromId(string splicerOutput, string id)
        {

            // input form: IDENTIFIER1=RESULT1|IDENTIFIER2=RESULT2|...   
            string pattern = @"(?<identifier>[^|=]+)=(?<result>[^|]*)";  
            
            foreach (Match match in Regex.Matches(splicerOutput, pattern))
            {
                string matchedId = match.Groups["identifier"].Value;
                string matchedResult = match.Groups["result"].Value; 
                             
                if (matchedId == id)
                {
                    return AutoParse(matchedResult);
                }
            }   

            return null;
        }
        
        public static Dictionary<string, object?> GetOutputAsDict(string query, string[] identifiers, bool concatenate)
        {
            // <summary> Takes the output of the splicer and formats it into a dict </summary>


            Dictionary<string, object?> pairs = new();
            string splicerOutput;

            if (concatenate) 
            {
                string newQuery = query;
                foreach (string id in identifiers) newQuery+=$"|{id}";
                splicerOutput = splicer.CommandAndReceiveText(newQuery);
            }
            else splicerOutput = splicer.CommandAndReceiveText(query);

            foreach (string id in identifiers) pairs[id] = GetSpecificResultFromId(splicerOutput, id);

            return pairs;
        }

        public static object? GetSingleResult(string query, string identifier, bool concatenate=false)
        {
            return GetOutputAsDict(query, [identifier], concatenate)[identifier];
        }
    }
}