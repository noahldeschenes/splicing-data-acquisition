
using System.Text.Json;
using System.Text.RegularExpressions;
using Utils;


namespace RecordSplicingResults
{
    /*
        Folder: date
        Files inside: JSON, images, settings, errors


        JSON format: Splicer info, Relevant splice settings, Volatile memory splice data, 
        Non-volatile memory splice data

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


                if (id.StartsWith("CROSSTALK")) continue; //special case

                
                // result can be a string, an int, or a float, so we auto-parse 
                // it for the sake of convenience/flexibility
                if (int.TryParse(result, out int resultAsInt)) pairs[id] = resultAsInt;
                else if (float.TryParse(result, out float resultAsFloat)) pairs[id] = resultAsFloat;
                else pairs[id] = result;

            }

            return pairs;
        }


        static string QuerySplicer(string query, string[] identifiers)
        {
            // <summary> Formats query and identifiers to be machine-readable for
            // the splicer; returns the splicer's output </summary>

            string input = query;
            
            foreach (string id in identifiers)
            {
                input+=$"|{id}";
            }

            string output = SplicerUtils.splicer.CommandAndReceiveText(input);
            // TODO: NAK handling?

            return output;

        }




        

    }
}

