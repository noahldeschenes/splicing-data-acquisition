using System.Diagnostics;

namespace SplicingDataAcquisition;

class SplicerUtils{


    public static UsbFsm100ServerClass splicer = new();
    public static void QuitIfDisconnected()
    {
        // UNTESTED

        if (!splicer.ConnectionStatus)
        {
            Console.WriteLine("Splicer disconnected. Now exiting...");
            Environment.Exit(0);
        }
    }

    public static bool InitializeAndLock(int timeout=10000)
    {
        /// <summary> Initializes splicer driver, locks controls </summary>  

        // UNTESTED

        // initializing driver and locking controls
        splicer.InitDriver(Process.GetCurrentProcess().Handle);
        splicer.Command("$LOCK");

        // waiting for an idle state 
        DateTime start_time = DateTime.Now;
        while ((DateTime.Now - start_time).TotalMilliseconds < timeout)
        {
            string current_status = splicer.CommandAndReceiveText("=FUNCSTAT");
            string[] idle_states = {"READY", "PAUSE1", "PAUSE2", "FINISH"};
            if (idle_states.Contains(current_status)) return true; 
            Thread.Sleep(100);
        }

        //unlocks if we never get an idle state
        splicer.Command("$UNLOCK");
        return false;

    }

    public static void SplicerOutputToHumanReadable(string result)
    {
        for (int i=0; i<result.Length; i++)
        {
            if (result[i] == 0x06) Console.Write("ACK");
            else if (result[i] == 0x15) Console.Write("NAK");
            Console.Write(result[i]);
        }
        Console.Write('\n');
    }

}