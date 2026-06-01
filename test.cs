using System.Diagnostics;


namespace Test
{
    class Program
    {

        static void Main(string[] args)
        {
            var server = new UsbFsm100ServerClass(Process.GetCurrentProcess().Handle);
            Console.WriteLine(server.ConnectionStatus);
            while (true)
            {
                Console.WriteLine(server.CommandAndReceiveText("=FUNCSTAT"));
                Thread.Sleep(3000);
            }
        }
        
    }
}