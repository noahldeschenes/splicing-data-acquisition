
namespace SplicingDataAcquisition
{
    class Program
    {
        public static var splicer = new UsbFsm100ServerClass(Process.GetCurrentProcess().Handle);
        public static void QuitIfDisconnected()
        {
            if (!splicer.ConnectionStatus)
            {
                Console.WriteLine("Splicer disconnected. Now exiting...");
                Environment.Exit(0);
            }
            
        }


        static void Main(string[] args)
        {
            
        }
    }
}