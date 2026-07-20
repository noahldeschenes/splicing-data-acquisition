
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace RecordSplicingResults
{
    internal static class OutputHandler
    {
        
        internal const int NUM_OF_MODES = 300; 
        internal const string NAK = "\x15"; // ASCII code for NAK
        internal static IUsbFsm100ServerClass splicer = new UsbFsm100ServerAdapter();
        
        /// <summary>
        /// Takes splicer output fields and parses them into their appropriate datatype
        /// (ints, floats, strings).
        /// </summary>
        /// <param name="result">Splicer output being parsed.</param>
        /// <returns>Splicer output correctly typed.</returns>
        private static object AutoParse(string result)
        {
            if (int.TryParse(result, out int resultAsInt)) return resultAsInt;
            else if (float.TryParse(result, out float resultAsFloat)) return resultAsFloat;
            else return result;
        }

        /// <summary>
        /// Uses a regex to match a given identifier with a given result.
        /// </summary>
        /// <param name="splicerOutput">The unprocessed output string from the splicer.</param>
        /// <param name="id">The identifier we want to extract from the output string.</param>
        /// <returns>The result associated with the given identifier.</returns>
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
        
        /// <summary>
        /// Sends a query to the splicer and parses the result into a dict.
        /// </summary>
        /// <param name="query">Query to send to the splicer.</param>
        /// <param name="identifiers">Identifiers to extract from splicer output.</param>
        /// <param name="concatenate">Whether the desired identifiers need to be concatenated 
        /// onto the end of the query string.</param>
        /// <returns>An identifier->result dict.</returns>
        internal static Dictionary<string, object?> GetOutputAsDict(string query, string[] identifiers, bool concatenate)
        {

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

        /// <summary>
        /// Wrapper for GetOutputAsDict.
        /// </summary>
        /// <param name="query">Query to send to the splicer.</param>
        /// <param name="identifier">Identifier to extract from splicer output.</param>
        /// <param name="concatenate">Whether the desired identifier need to be concatenated 
        /// onto the end of the query string.</param>
        /// <returns>Result associated with the identifier.</returns>
        internal static object? GetSingleResult(string query, string identifier, bool concatenate=false)
        {
            return GetOutputAsDict(query, [identifier], concatenate)[identifier];
        }
    }
}