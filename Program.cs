
using System.Diagnostics;

namespace SplicingDataAcquisition;

class Program
{ 
    
    
    static void Main(string[] args)
    {
        SplicerUtils.splicer.InitDriver(Process.GetCurrentProcess().Handle);
        SplicerUtils.QuitIfDisconnected();
        BackupUtils.Backup();
    }

}



