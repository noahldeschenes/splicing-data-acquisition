


namespace Test
{
    class Program
    {

        static void Main(string[] args)
        {
            var server = new UsbFsm100ServerClass();
            Console.WriteLine(server.ConnectionStatus);
        }
    }
}