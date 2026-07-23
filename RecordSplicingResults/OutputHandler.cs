
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;


namespace RecordSplicingResults
{
    internal static class OutputHandler
    {
        
        
        internal const string NAK = "\x15"; // ASCII code for NAK
        internal static IUsbFsm100ServerClass splicer = new MockUsbFsm100ServerClass([],[],[],[],[]);

        /// <summary>
        /// Thrown when a query to the splicer returns an invalid output or a NAK
        /// </summary>
        public class SplicerQueryFailedException : Exception
        {
            public SplicerQueryFailedException(string message) : base(message) { }
        }
        


        /// <summary>
        /// Takes splicer output fields and parses them into their appropriate datatype
        /// (ints, floats, strings).
        /// </summary>
        /// <param name="result">Splicer output being parsed.</param>
        /// <returns>Splicer output correctly typed.</returns>
        internal static object AutoParse(string result)
        {

            Match match = Regex.Match(result, @"^-?\d+\.\d+");
            if (match.Success)
            {
                if (float.TryParse(match.Value, out float resultAsFloat)) return resultAsFloat;
            }

            match = Regex.Match(result, @"^-?\d+");
            if (match.Success)
            {
                if (int.TryParse(match.Value, out int resultAsInt)) return resultAsInt;
            }
            
            
            char[] invalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];
            return string.Concat(result.Split(invalidChars));
        }

        /// <summary>
        /// Uses a regex to match a given identifier with a given result.
        /// </summary>
        /// <param name="splicerOutput">The unprocessed output string from the splicer.</param>
        /// <param name="id">The identifier we want to extract from the output string.</param>
        /// <returns>The result associated with the given identifier.</returns>
        internal static object? GetSpecificResultFromId(string splicerOutput, string id)
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
        /// Gets a single result from the splicer in an IDENTIFIER|RESULT pair.
        /// </summary>
        /// <param name="query">Query to send to the splicer.</param>
        /// <param name="identifier">Identifier to extract from splicer output.</param>
        /// <param name="concatenate">Whether the identifier needs to be concatenated 
        /// onto the end of the query string.</param>
        /// <returns>Result associated with the identifier.</returns>
        internal static object GetSingleResult(string query, string identifier, bool concatenate=false)
        {
            string splicerOutput;

            if (concatenate) query += $"|{identifier}";
            splicerOutput = splicer.CommandAndReceiveText(query);

            object? result = GetSpecificResultFromId(splicerOutput, identifier);
            
            if (result is null) throw new SplicerQueryFailedException($@"Unable to get value for {identifier} from splicer. 
            Splicer may have been interrupted by keypad inputs (i.e. SET).");
            
            return result!;
        }
        
        /// <summary>
        /// Sends a query to the splicer and parses the result into a dict.
        /// </summary>
        /// <param name="query">Query to send to the splicer.</param>
        /// <param name="identifiers">Identifiers to extract from splicer output.</param>
        /// <param name="concatenate">Whether the identifiers need to be concatenated 
        /// onto the end of the query string.</param>
        /// <returns>An identifier->result dict.</returns>
        internal static Dictionary<string, object> GetOutputAsDict(string query, string[] identifiers, bool concatenate)
        {

            Dictionary<string, object> pairs = new();
            string splicerOutput;

            if (concatenate) 
            {
                string newQuery = query;
                foreach (string id in identifiers) newQuery+=$"|{id}";
                splicerOutput = splicer.CommandAndReceiveText(newQuery);
            }
            else splicerOutput = splicer.CommandAndReceiveText(query);

            foreach (string id in identifiers) pairs[id] = GetSpecificResultFromId(splicerOutput, id) ?? "";

            return pairs;
        }

        
    }
}