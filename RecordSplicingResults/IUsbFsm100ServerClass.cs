using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms.VisualStyles;

namespace RecordSplicingResults
{
    /// <summary>
    /// An interface used for dependency inversion for the UsbFsm100ServerClass class.
    /// </summary>
    internal interface IUsbFsm100ServerClass
    {
        
        bool ConnectionStatus { get; }
        void InitDriver(IntPtr handle);
        string CommandAndReceiveText(string command);
        byte[] CommandAndReceiveBinary(string command);
        void SendBinary(ref byte[] data, int length, int timeout);

    }

    /// <summary>
    /// An adapter class for the real splicer, which implements IUsbFsm100ServerClass.
    /// </summary>
    internal class UsbFsm100ServerAdapter : IUsbFsm100ServerClass
    {
        private readonly UsbFsm100ServerClass _splicer;

        public UsbFsm100ServerAdapter()
        {
            _splicer = new UsbFsm100ServerClass();
        }

        public bool ConnectionStatus => _splicer.ConnectionStatus;

        public void InitDriver(IntPtr handle)
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

    /// <summary>
    /// A mock splicer for use in testing.
    /// </summary>
    internal class MockUsbFsm100ServerClass : IUsbFsm100ServerClass
    {
        public IEnumerator<string> commandAndReceiveTextValidInputs {get; set;}
        public IEnumerator<string> commandAndReceiveTextReturnValues {get; set;}
        public IEnumerator<string> commandAndReceiveBinaryValidInputs {get; set;}
        public IEnumerator<byte[]> commandAndReceiveBinaryReturnValues {get; set;}
        public IEnumerator<byte[]> sendBinaryValidInputs {get; set;}
        public bool sendBinaryStrict {get; set;}
        public bool driverInitialized {get; set;}


        public bool ConnectionStatus => driverInitialized;

        public MockUsbFsm100ServerClass(string[] crtvi, string[] crtrv, string[] crbvi, byte[] crbrv, byte[] sbvi, bool sbs = true)
        {
            commandAndReceiveTextValidInputs = (IEnumerator<string>) crtvi.GetEnumerator();
            commandAndReceiveTextReturnValues = (IEnumerator<string>) crtrv.GetEnumerator();
            commandAndReceiveBinaryValidInputs = (IEnumerator<string>) crbvi.GetEnumerator();
            commandAndReceiveBinaryReturnValues = (IEnumerator<byte[]>) crbrv.GetEnumerator();
            sendBinaryValidInputs = (IEnumerator<byte[]>) sbvi.GetEnumerator();
            sendBinaryStrict = sbs;
            driverInitialized = false;
        }

        /// <summary>
        /// Mock InitDriver method.
        /// </summary>
        /// <param name="handle">Handle to the current process.</param>
        public void InitDriver(IntPtr handle)
        {
            driverInitialized = true;
        }

        /// <summary>
        /// Uses input+output streams to act like CommandAndReceiveText().
        /// </summary>
        /// <param name="command">Command being sent to the mock splicer.</param>
        /// <returns>A mock string formatted like the splicer's return strings.</returns>
        /// <exception cref="InvalidOperationException">Method was called unexpectedly.</exception>
        /// <exception cref="ArgumentException">Input to method did not match input stream.</exception>
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
        
        /// <summary>
        /// Uses input+output streams to act like CommandAndReceiveBinary().
        /// </summary>
        /// <param name="command">Command being sent to the mock splicer.</param>
        /// <returns>A mock binary array formatted like the splicer's return binary arrays.</returns>
        /// <exception cref="InvalidOperationException">Method was called unexpectedly.</exception>
        /// <exception cref="ArgumentException">Input to method did not match input stream.</exception>
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

        /// <summary>
        /// Uses and input stream to act like SendBinary()
        /// </summary>
        /// <param name="data">Data (likely splice parameters) being sent to the splicer.</param>
        /// <param name="length">Length of the data array.</param>
        /// <param name="timeout">Dummy timeout value.</param>
        /// <exception cref="InvalidOperationException">Method was called unexpectedly.</exception>
        /// <exception cref="ArgumentException">Input to method did not match input stream.</exception>
        public void SendBinary(ref byte[] data, int length, int timeout)
        {
            if (!sendBinaryStrict) return;
            if (!sendBinaryValidInputs.MoveNext())
            {
                throw new InvalidOperationException("SendBinary() was called unexpectedly.");
            }
            else if (!data.SequenceEqual(sendBinaryValidInputs.Current)||data.Length!=length)
            {
                throw new ArgumentException($@"Invalid input for SendBinary().");
            }
        }
    }
}