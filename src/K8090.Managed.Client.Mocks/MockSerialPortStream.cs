using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace K8090.ManagedClient.Mocks
{
    public class MockSerialPortStream : ISerialPortStream
    {
        private MockRelayProtocol _protocol;
        private bool _connected = false;
        private string _portName = "COM4";
        private Handshake _handshake;
        private byte[] _data;

        #region STUFF WE USE
        public string PortName { get => _portName; set => _portName = value; }
        public bool IsOpen => _connected;
        public Handshake Handshake { get => _handshake; set => _handshake = value; }

        public event EventHandler<SerialDataReceivedEventArgs> DataReceived;
        public event EventHandler<SerialErrorReceivedEventArgs> ErrorReceived;

        public void PressButton(int buttonIndex, TimeSpan holdFor)
        {
            _protocol.PressButton(buttonIndex, holdFor);
        }

        public void Open()
        {
            _connected = true;
        }

        public void Close()
        {
            _connected = false;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (count != 7) throw new ArgumentException("Packet size must be 7 bytes");
           
            GetResponseFor(buffer);
            if (_data?.Length > 0) DataReceived?.Invoke(this, new SerialDataReceivedEventArgs(SerialData.Chars));
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_data == null) return 0;

            int byteCount = 0;
            foreach(byte b in _data)
            {
                buffer[offset+byteCount++] = b;
                count--;
                if (count == 0) break;
            }

            return byteCount;
        }

        public void InvokeDataReceived(byte[] data)
        {
            _data = data;
            DataReceived?.Invoke(this, new SerialDataReceivedEventArgs(SerialData.Chars));
        }

        public void InvokeErrorReceived(SerialError eventType)
        {
            ErrorReceived?.Invoke(this, new SerialErrorReceivedEventArgs(eventType));
        }

        public void Dispose()
        {
        }

        #endregion

        private void GetResponseFor(byte[] data)
        {
            _data = _protocol.GetResponseFor(data);
        }

        public MockSerialPortStream()
        {
            _protocol = new MockRelayProtocol(this);
        }

        #region STUFF WE DON'T USE
        public string Version => throw new NotImplementedException();
        public Encoding Encoding { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string NewLine { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int DriverInQueue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int DriverOutQueue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool CanTimeout => throw new NotImplementedException();

        public bool CanRead => throw new NotImplementedException();

        public int ReadTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int ReadBufferSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int ReceivedBytesThreshold { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int BytesToRead => throw new NotImplementedException();

        public bool CanWrite => throw new NotImplementedException();

        public int WriteTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int WriteBufferSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int BytesToWrite => throw new NotImplementedException();

        public bool CDHolding => throw new NotImplementedException();

        public bool CtsHolding => throw new NotImplementedException();

        public bool DsrHolding => throw new NotImplementedException();

        public bool RingHolding => throw new NotImplementedException();

        public int BaudRate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int DataBits { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public StopBits StopBits { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Parity Parity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public byte ParityReplace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool DiscardNull { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool DtrEnable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool RtsEnable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int XOnLimit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int XOffLimit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool TxContinueOnXOff { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool BreakState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool CanSeek => throw new NotImplementedException();

        public long Length => throw new NotImplementedException();

        public long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsDisposed => throw new NotImplementedException();

        public event EventHandler<SerialPinChangedEventArgs> PinChanged;

        public void CopyTo(Stream destination)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Stream destination, int bufferSize)
        {
            throw new NotImplementedException();
        }

        public void DiscardInBuffer()
        {
            throw new NotImplementedException();
        }

        public void DiscardOutBuffer()
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void GetPortSettings()
        {
            throw new NotImplementedException();
        }

        public void OpenDirect()
        {
            throw new NotImplementedException();
        }

        public int Read(char[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public int ReadByte()
        {
            throw new NotImplementedException();
        }

        public int ReadChar()
        {
            throw new NotImplementedException();
        }

        public string ReadExisting()
        {
            throw new NotImplementedException();
        }

        public string ReadLine()
        {
            throw new NotImplementedException();
        }

        public string ReadTo(string text)
        {
            throw new NotImplementedException();
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public void Write(char[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public void Write(string text)
        {
            throw new NotImplementedException();
        }

        public void WriteByte(byte value)
        {
            throw new NotImplementedException();
        }

        public void WriteLine(string text)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}