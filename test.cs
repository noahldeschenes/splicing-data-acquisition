


namespace Test
{
    class Program
    {

        static void Main(string[] args)
        {
            var server = new UsbFsm100ServerClass();
            server.InitDriver(IntPtr.Zero);
            Console.WriteLine(server.ConnectionStatus);
        }
    }
}