using System.Diagnostics;


namespace SplicingDataAcquisition
{
    class Test
    {

        static void Main(string[] args)
        {
            var server = new UsbFsm100ServerClass(Process.GetCurrentProcess().Handle);
            Console.WriteLine(server.ConnectionStatus);
            while (true)
            {
                Console.WriteLine(server.CommandAndReceiveText("$RESET"));
                Thread.Sleep(3000);
            }
        }

    }
}