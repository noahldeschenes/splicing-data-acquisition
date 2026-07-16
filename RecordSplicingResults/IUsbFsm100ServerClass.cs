

namespace RecordSplicingResults
{
    public interface IUsbFsm100ServerClass
    {
        bool ConnectionStatus { get; }
        void InitDriver(IntPtr handle);
        string CommandAndReceiveText(string command);
        byte[] CommandAndReceiveBinary(string command);
        void SendBinary(ref byte[] data, int length, int timeout);
    }
}