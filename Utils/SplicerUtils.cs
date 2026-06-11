

using System.Diagnostics;

namespace Utils
{
    public static class SplicerUtils
    {
        public const string LOG_FILE_LOCATION = @"C:\Users\noah.deschenes\Documents\error_log.txt"; // temporary 
        public static UsbFsm100ServerClass splicer = new();
        public const int MAX_MODENO = 300; // splicer has modes numbered 1-300
        public const char NAK = '\x15'; // ASCII code for NAK character

        public static void InitializeAndLock(int timeout=100000)
        {
            /// <summary> Initializes splicer driver, locks controls </summary>  

            // initializing driver and locking controls
            splicer.InitDriver(Process.GetCurrentProcess().Handle);
            QuitIfDisconnected();
            splicer.Command("$LOCK");

            // waiting for an idle state 
            DateTime start_time = DateTime.Now;
            while ((DateTime.Now - start_time).TotalMilliseconds < timeout)
            {
                string current_status = splicer.CommandAndReceiveText("=FUNCSTAT");
                string[] idle_states = {"READY", "PAUSE1", "PAUSE2", "FINISH"};
                if (idle_states.Contains(current_status)) return;
                Thread.Sleep(100);
            }

            using (StreamWriter sw = File.AppendText(LOG_FILE_LOCATION))
            {
                sw.WriteLine($"Splicer in use at {DateTime.UtcNow}.");
            }

            splicer.Command("$UNLOCK");
            Environment.Exit(0);

        }

        public static void QuitIfDisconnected()
        {

            if (!splicer.ConnectionStatus)
            {
                using (StreamWriter sw = File.AppendText(LOG_FILE_LOCATION))
                {
                    sw.WriteLine(@$"Splicer disconnected at {DateTime.UtcNow}.
                    Check connection and/or restart the splicer.");
                }
                
                Environment.Exit(0);
            }
        }
        public static void QuitIfNAK(string result)
        {
            if (result.Length > 0 && result[0] == NAK)
            {
                using (StreamWriter sw = File.AppendText(LOG_FILE_LOCATION))
                {
                    sw.WriteLine($"Received NAK from splicer at {DateTime.UtcNow}.");
                }

                splicer.Command("$UNLOCK");
                Environment.Exit(0);
            }
        }

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

    }
}