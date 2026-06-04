

namespace Utils
{
    public static class ErrorHandling
    {
        public const string LOG_FILE_LOCATION = @"C:\Users\noah.deschenes\Documents\error_log.txt"; // temporary 

        public static void InitializeAndLock(int timeout=100000)
        {
            /// <summary> Initializes splicer driver, locks controls </summary>  

            // initializing driver and locking controls
            splicer.InitDriver(Process.GetCurrentProcess().Handle);
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

            //unlocks if we never get an idle state
            splicer.Command("$UNLOCK");
            
            using (StreamWriter sw = File.AppendText(LOG_FILE_LOCATION))
            {
                sw.WriteLine($"Unable to initialize splicer at {DateTime.UtcNow}.");
            }

            Environment.Exit(0);

        }

        public static void QuitIfDisconnected()
        {
            // UNTESTED

            if (!splicer.ConnectionStatus)
            {
                using (StreamWriter sw = File.AppendText(LOG_FILE_LOCATION))
                {
                    sw.WriteLine($"Splicer disconnected at {DateTime.UtcNow}.");
                }
                Environment.Exit(0);
            }
        }
        public static void QuitIfNAK(string result)
        {
            if (result.StartsWith("\x15")) // 0x15 is NAK
            {
                using (StreamWriter sw = File.AppendText(LOG_FILE_LOCATION))
                {
                    sw.WriteLine($"Received NAK from splicer at {DateTime.UtcNow}.");
                }
                Environment.Exit(0);
            }
        }

    }
}