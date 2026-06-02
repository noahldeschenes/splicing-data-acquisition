

static void QuitIfDisconnected()
{
    if (!splicer.ConnectionStatus)
    {
        Console.WriteLine("Splicer disconnected. Now exiting...");
        Environment.Exit(0);
    }
}

static bool LockAndPoll(int timeout)
{
    splicer.Lock();
    DateTime start_time = DateTime.Now;
    while ((DateTime.Now - start_time).TotalMilliseconds < timeout)
    {
        string current_status = splicer.CommandAndReceiveText("=FUNCSTAT");
        valid_states = new string[] {"READY", "PAUSE1", "PAUSE2", "FINISH"};
        if (valid_states.Contains(current_status)) return true; 
        Thread.Sleep(100);
    }

    return false;

}
