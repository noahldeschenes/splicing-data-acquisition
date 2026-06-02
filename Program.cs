using System.Diagnostics;

namespace SplicingDataAcquisition;

class Program
{ 
    
    
    static void Main(string[] args)
    {
        SplicerUtils.splicer.InitDriver(Process.GetCurrentProcess().Handle);
        SplicerUtils.QuitIfDisconnected();
        byte[] backup = SplicerUtils.splicer.CommandAndReceiveBinary("%SPLH-28");
        SplicerUtils.splicer.Command("SPLH-60");
        SplicerUtils.splicer.SendBinary(ref backup, backup.Length, 1000);

    }

}



