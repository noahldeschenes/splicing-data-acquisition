using System;
using System.Collections;
using System.Collections.Generic;

namespace RecordSplicingResults
{
    public interface IUsbFsm100ServerClass
    {
        bool ConnectionStatus { get; }
        void InitDriver(System.IntPtr handle);
        string CommandAndReceiveText(string command);
        byte[] CommandAndReceiveBinary(string command);
        void SendBinary(ref byte[] data, int length, int timeout);
    }

    public class UsbFsm100ServerAdapter : IUsbFsm100ServerClass
    {
        private readonly UsbFsm100ServerClass _splicer;

        public UsbFsm100ServerAdapter()
        {
            _splicer = new UsbFsm100ServerClass();
        }

        public bool ConnectionStatus => _splicer.ConnectionStatus;

        public void InitDriver(System.IntPtr handle)
        {
            _splicer.InitDriver(handle);
        }

        public string CommandAndReceiveText(string command)
        {
            return _splicer.CommandAndReceiveText(command);
        }

        public byte[] CommandAndReceiveBinary(string command)
        {
            return _splicer.CommandAndReceiveBinary(command);
        }

        public void SendBinary(ref byte[] data, int length, int timeout)
        {
            _splicer.SendBinary(ref data, length, timeout);
        }
    }

    public class DummyUsbFsm100ServerClass : IUsbFsm100ServerClass
    {
        IEnumerator<string> commandAndReceiveTextValidInputs = new List<string>().GetEnumerator();
        IEnumerator<string> commandAndReceiveTextReturnValues = new List<string>().GetEnumerator();
        IEnumerator<string> commandAndReceiveBinaryValidInputs = new List<string>().GetEnumerator();
        IEnumerator<byte[]> commandAndReceiveBinaryReturnValues = new List<byte[]>().GetEnumerator();
        IEnumerator<byte[]> sendBinaryValidInputs = new List<byte[]>().GetEnumerator();
        bool sendBinaryStrict = true;
        bool driverInitialized = false;


        public bool ConnectionStatus => driverInitialized;

        public void InitDriver(System.IntPtr handle)
        {
            driverInitialized = true;
        }

        public string CommandAndReceiveText(string command)
        {
            if (!commandAndReceiveTextValidInputs.MoveNext() || !commandAndReceiveTextReturnValues.MoveNext())
            {
                throw new InvalidOperationException("CommandAndRecieveText() was called unexpectedly.");
            }
            else if (command != commandAndReceiveTextValidInputs.Current)
            {
                throw new ArgumentException($@"Invalid input for CommandAndReceiveText(). Expected: 
                {commandAndReceiveTextValidInputs.Current}, Received: {command}");
            }
            return commandAndReceiveTextReturnValues.Current;
        }

        public byte[] CommandAndReceiveBinary(string command)
        {
            if (!commandAndReceiveBinaryValidInputs.MoveNext() || !commandAndReceiveBinaryReturnValues.MoveNext())
            {
                throw new InvalidOperationException("CommandAndRecieveBinary() was called unexpectedly.");
            }
            else if (command != commandAndReceiveBinaryValidInputs.Current)
            {
                throw new ArgumentException($@"Invalid input for CommandAndReceiveBinary(). Expected: 
                {commandAndReceiveBinaryValidInputs.Current}, Received: {command}");
            }
            return commandAndReceiveBinaryReturnValues.Current;
        }

        public void SendBinary(ref byte[] data, int length, int timeout)
        {
            if (!sendBinaryStrict) return;
            if (!sendBinaryValidInputs.MoveNext())
            {
                throw new InvalidOperationException("SendBinary() was called unexpectedly.");
            }
            else if (!data.SequenceEqual(sendBinaryValidInputs.Current))
            {
                throw new ArgumentException($@"Invalid input for SendBinary().");
            }
        }
    }
}