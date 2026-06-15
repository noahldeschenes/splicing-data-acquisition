

using System.Diagnostics;
using System.Text.RegularExpressions;


namespace Utils
{
    public static class SplicerUtils
    {
        public const string LOG_FILE_LOCATION = @"C:\Users\noah.deschenes\Documents\error_log.txt"; // temporary 
        public static UsbFsm100ServerClass splicer = new();
        public const int MAX_MODENO = 300; // splicer has modes numbered 1-300
        public const char NAK = '\x15'; // ASCII code for NAK character

        public static readonly int POLLING_WAIT_TIME = 1000;

        public static string QuerySplicer(string query, string[] identifiers)
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


        public static Dictionary<string, object> GetOutputAsDict(string query, string[] identifiers)
        {
            // <summary> Takes the output of the splicer and formats it into a dict </summary>

            // TODO: change to return null if there's a NAK

            
            string splicerOutput = QuerySplicer(query, identifiers);

            Dictionary<string, object> pairs = new();

            string pattern = @"(?<identifier>[^|=]+)=(?<result>[^|]*)";  // input form: IDENTIFIER1=RESULT1|IDENTIFIER2=RESULT2|...   

            foreach (Match match in Regex.Matches(splicerOutput, pattern))
            {
                string id = match.Groups["identifier"].Value;
                string result = match.Groups["result"].Value; 

                // result can be a string, an int, or a float, so we auto-parse 
                // it for the sake of convenience/flexibility
                if (int.TryParse(result, out int resultAsInt)) pairs[id] = resultAsInt;
                else if (float.TryParse(result, out float resultAsFloat)) pairs[id] = resultAsFloat;
                else pairs[id] = result;

            }

            return pairs;
        }
        public static void AcquireSplicerLock()
        {
            WaitForConnection();
            
            while (true)
            {
                string currentStatus = SplicerUtils.QuerySplicer("=FUNCSTAT", []);
                if (currentStatus != "READY" && currentStatus != "FINISH") continue;
                
                SplicerUtils.splicer.Command("LOCK");
                
                if (currentStatus != "READY" && currentStatus != "FINISH") {
                    SplicerUtils.splicer.Command("UNLOCK");
                    continue;
                }

                break;
            }

        }
        public static void WaitForConnection()
        {
            while (true)
            {
                if (SplicerUtils.splicer.ConnectionStatus) break;
                SplicerUtils.splicer.InitDriver(Process.GetCurrentProcess().Handle);
                Thread.Sleep(POLLING_WAIT_TIME);
            }
        }
    }
}