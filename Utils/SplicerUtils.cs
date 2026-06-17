

using System.Diagnostics;
using System.Text.RegularExpressions;


namespace Utils
{
    public static class SplicerUtils
    {
        public const string LOG_FILE_LOCATION = @"C:\Users\noah.deschenes\Documents\error_log.txt"; // temporary 
        public static UsbFsm100ServerClass splicer = new();
        public const int MAX_MODENO = 300; // splicer has modes numbered 1-300
        public const char NAK = '\x15'; // ASCII code for NAK

        public static readonly int POLLING_WAIT_TIME = 250;

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
            
            return output;

        }

        public static void QuitIfNAK(string result)
        {
            if (result.Length > 0 && result[0] == NAK)
            {
                throw new Exception("NAK");
            }
        }

        
        public static void AcquireSplicerLock()
        {
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