using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RJCP.IO.Ports;

namespace K8090.ManagedClient
{

    public class RelayCard : IDisposable
    {
        public const int PACKET_SIZE = 7; // each packet is 7 bytes (see datapacket.cs for the definition)
        public const int BUFFER_SIZE = PACKET_SIZE * 8; // in some cases we may get up to 8 packets in a single operation
        public const int WAIT_TIMEOUT_IN_MILLISECONDS = 3000;

        protected ISerialPortStream _serialPort;
        private byte[] _buffer = new byte[BUFFER_SIZE];
        private byte _relayState = 0;
        private byte _timerActive = 0;
        private bool _ignoreResponses = false;
        private bool _responseReceived = false;
        private IEnumerable<DataPacket> _packetsReceived;

        public bool Connected => _serialPort.IsOpen;
        public bool[] RelayState => _relayState.GetBits(0, 8);

        public event EventHandler<RelayStatus> OnRelayStateChanged;
        public event EventHandler<ButtonStatus> OnButtonStateChanged;
        public event EventHandler<RelayStatus> OnRelayTimerStarted;
        public event EventHandler<RelayStatus> OnRelayTimerExpired;

        public void Connect()
        {
            try
            {
                _serialPort.Open();

                // get the initial relay state as some relays may already be on
                _ignoreResponses = true;
                _relayState = SendCommandAndAwaitResponse(Command.QueryRelayState, Response.RelayStatus)?.Param1 ?? 0;
                _ignoreResponses = false;
            }
            catch (InvalidOperationException ex)
            {
                _serialPort.Close();
                _serialPort.Dispose();

                // should be able to work this out better (card not present etc) by examining the exception type... 
                throw new ConnectionException("Could not connect to the K8090 board. See InnerException for details.", ex);
            }
        }

        public void Disconnect()
        {
            _serialPort.Close();
        }

        public void Reset()
        {
            EnsureConnected();

            _ignoreResponses = true; // suppresses event generation but allows queued responses to clear
            SetRelaysOff(0, 1, 2, 3, 4, 5, 6, 7);
            _relayState = 0;
            _ignoreResponses = false;
        }

        public void SetRelayOn(int relayIndex)
        {
            if (relayIndex > 7 || relayIndex < 0) return;
            SendCommand(Command.RelayOn, MaskFor(relayIndex));
        }

        public void SetRelaysOn(params int[] relayIndexes)
        {
            byte mask = MaskFor(relayIndexes.Where(x => x >= 0 && x < 8).ToArray());
            SendCommand(Command.RelayOn, mask);
        }

        public void SetRelayOff(int relayIndex)
        {
            if (relayIndex > 7 || relayIndex < 0) return;
            SendCommand(Command.RelayOff, MaskFor(relayIndex));
        }

        public void SetRelaysOff(params int[] relayIndexes)
        {
            byte mask = MaskFor(relayIndexes.Where(x => x >= 0 && x < 8).ToArray());
            SendCommand(Command.RelayOff, mask);
        }

        public void ToggleRelay(int relayIndex)
        {
            if (relayIndex > 7 || relayIndex < 0) return;
            SendCommand(Command.RelayToggle, MaskFor(relayIndex));
        }

        public void ToggleRelays(params int[] relayIndexes)
        {
            byte mask = MaskFor(relayIndexes.Where(x => x >= 0 && x < 8).ToArray());
            SendCommand(Command.RelayToggle, mask);
        }

        public IDictionary<int, RelayStatus> GetRelayStatus()
        {
            return BuildRelayStatus(SendCommandAndAwaitResponse(Command.QueryRelayState, Response.RelayStatus));
        }

        public IDictionary<int, ButtonMode> GetButtonModes()
        {
            Dictionary<int, ButtonMode> buttonModes = new Dictionary<int, ButtonMode>();
            DataPacket packet = SendCommandAndAwaitResponse(Command.QueryButtonMode, Response.QueryButtonMode);
            for (int i = 0; i < 8; i++)
            {
                ButtonMode mode = packet.Mask.GetBit(i) ? ButtonMode.Momentary : 
                    packet.Param1.GetBit(i) ? ButtonMode.Toggle : 
                    ButtonMode.Timer;
                buttonModes.Add(i, mode);
            }

            return buttonModes;
        }

        public void SetButtonModes(ButtonMode mode)
        {
            byte momentary = (byte)(mode == ButtonMode.Momentary ? 0xFF : 0x00);
            byte timer = (byte)(mode == ButtonMode.Timer ? 0xFF : 0x00);
            byte toggle = (byte)(mode == ButtonMode.Toggle ? 0xFF : 0x00);
            SendCommand(Command.SetButtonMode, momentary, toggle, timer);
        }

        public void SetButtonModes(IDictionary<int, ButtonMode> modes)
        {
            if (modes.Count() != 8) throw new ManagedClientException("Too few / too many ButtonMode values supplied. There must be 8 values.");

            byte momentary = 0;
            byte timer = 0; 
            byte toggle = 0;
            foreach(var modePair in modes)
            {
                int buttonIndex = modePair.Key;
                ButtonMode mode = modePair.Value;
                if (buttonIndex > 7 || buttonIndex < 0) break;
                if (mode == ButtonMode.Momentary) momentary = momentary.SetBit(buttonIndex, true);
                if (mode == ButtonMode.Toggle) toggle = toggle.SetBit(buttonIndex, true);
                if (mode == ButtonMode.Timer) timer = timer.SetBit(buttonIndex, true);
            }

            SendCommand(Command.SetButtonMode, momentary, toggle, timer);
        }

        public void StartRelayTimers(params int[] relayIndexes)
        {
            SetAndStartRelayTimers(0, relayIndexes);
        }

        public void SetAndStartRelayTimers(ushort delayInSeconds, params int[] relayIndexes)
        {
            byte relays = 0;
            foreach (int relayIndex in relayIndexes)
            {
                if (relayIndex < 8 && relayIndex >= 0)
                {
                    relays = relays.SetBit(relayIndex, true);
                    _timerActive = _timerActive.SetBit(relayIndex, true);
                }
            }

            SendCommand(Command.StartRelayTimer, relays, delayInSeconds.HighByte(), delayInSeconds.LowByte());
        }

        public void SetRelayTimersDefaultDelay(ushort delayInSeconds, params int[] relayIndexes)
        {
            byte relays = 0;
            foreach (int relayIndex in relayIndexes)
            {
                if (relayIndex < 8 && relayIndex >= 0)
                {
                    relays = relays.SetBit(relayIndex, true);
                }
            }

            SendCommand(Command.SetRelayTimerDelay, relays, delayInSeconds.HighByte(), delayInSeconds.LowByte());
        }

        public IDictionary<int, ushort> GetRelayTimerDelays(params int[] relayIndexes)
        {
            return GetRelayTimerDelays(false, relayIndexes);
        }

        public IDictionary<int, ushort> GetRelayTimerDelaysRemaining(params int[] relayIndexes)
        {
            return GetRelayTimerDelays(true, relayIndexes);
        }

        public string GetFirmwareVersion()
        {
            DataPacket packet = SendCommandAndAwaitResponse(Command.FirmwareVersion, Response.FirmwareVersion);
            return $"{ packet.Param1.ToString() }.{ packet.Param2.ToString() }";
        }

        public void ResetFactoryDefaults()
        {
            SendCommand(Command.ResetFactoryDefaults);
        }

        public bool IsEventJumperSet()
        {
            return (SendCommandAndAwaitResponse(Command.JumperStatus, Response.JumperStatus).Param1 > 1);
        }

        private IDictionary<int, RelayStatus> BuildRelayStatus(DataPacket packet)
        {
            Dictionary<int, RelayStatus> statusList = new Dictionary<int, RelayStatus>();

            for (int i = 0; i < 8; i++)
            {
                RelayStatus status = new RelayStatus(
                    i,
                    packet.Mask.GetBit(i),
                    packet.Param1.GetBit(i),
                    packet.Param2.GetBit(i)
                    );
                statusList.Add(i, status);
            }

            return statusList;
        }

        private IDictionary<int, ButtonStatus> BuildButtonStatus(DataPacket packet)
        {
            Dictionary<int, ButtonStatus> statusList = new Dictionary<int, ButtonStatus>();

            for (int i = 0; i < 8; i++)
            {
                ButtonStatus status = new ButtonStatus(
                    i,
                    packet.Mask.GetBit(i),
                    packet.Param1.GetBit(i),
                    packet.Param2.GetBit(i)
                    );
                statusList.Add(i, status);
            }

            return statusList;
        }

        private IDictionary<int, ushort> GetRelayTimerDelays(bool remainingDelay, params int[] relayIndexes)
        {
            byte relays = 0;
            foreach (int relayIndex in relayIndexes)
            {
                if (relayIndex < 8 && relayIndex >= 0)
                {
                    relays = relays.SetBit(relayIndex, true);
                }
            }

            SendCommand(Command.QueryRelayTimerDelay, relays, (byte)(remainingDelay ? 2 : 1));
            IEnumerable<DataPacket> packets = SendCommandAndAwaitResponses(Command.QueryRelayTimerDelay, Response.QueryRelayTimerDelay);

            Dictionary<int, ushort> delays = new Dictionary<int, ushort>();
            foreach (DataPacket packet in packets)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (packet.Mask.GetBit(i))
                    {
                        delays.Add(i, (packet.Param2, packet.Param1).ToWord());
                    }
                }
            }

            return delays;
        }

        private void SendCommand(Command command, byte mask = 0, byte param1 = 0, byte param2 = 0)
        {
            EnsureConnected();

            DataPacket packet = new DataPacket(command, mask, param1, param2);
            _serialPort.Write(packet.AsByteArray, 0, 7);
            _serialPort.Flush();
        }

        private DataPacket SendCommandAndAwaitResponse(Command command, Response response, byte mask = 0, byte param1 = 0, byte param2 = 0)
        {
            return SendCommandAndAwaitResponses(command, response, mask, param1, param2).FirstOrDefault();
        }

        private IEnumerable<DataPacket> SendCommandAndAwaitResponses(Command command, Response response, byte mask = 0, byte param1 = 0, byte param2 = 0)
        {
            IEnumerable<DataPacket> packets = SendCommandAndAwaitAllResponses(command, mask, param1, param2);
            if (response != Response.None)
            {
                command = (Command)response; // response is a subset of command with the same cardinal values, so directly convertible
            }

            return packets.Where(x => x.Command == command);
        }

        private IEnumerable<DataPacket> SendCommandAndAwaitAllResponses(Command command, byte mask = 0, byte param1 = 0, byte param2 = 0)
        {
            SendCommand(command, mask, param1, param2);
            return AwaitAllResponses();
        }

        private IEnumerable<DataPacket> AwaitAllResponses()
        {
            List<DataPacket> packets = new List<DataPacket>();
            DateTime started = DateTime.Now;
            while (!_responseReceived && !_ignoreResponses)
            {
                // loop and wait until some data has been received in the DataReceived event handler below,
                // then we pass it back up the method chain to the caller in the correctly filtered form
                // if a timeout threshold is passed, we abandon ship - this shouldn't happen unless the board
                // was disconnected mid-operation...

                if (DateTime.Now.Subtract(started) > TimeSpan.FromMilliseconds(WAIT_TIMEOUT_IN_MILLISECONDS))
                {
                    _responseReceived = false;
                    throw new ManagedClientException("Wait for reply from K8090 board timed out. Check connection.");
                }

                Thread.Sleep(1);
            }

            _responseReceived = false;
            return (_ignoreResponses ? new DataPacket[0] : _packetsReceived);
        }

        private void ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!_ignoreResponses)
            {
                List<DataPacket> packets = new List<DataPacket>();

                // there may be between 1 and 8 packets queued up, depending on the operation
                // which prompted them, so we must be ready to read all of them

                int read = _serialPort.Read(_buffer, 0, BUFFER_SIZE);
                if (read >= PACKET_SIZE)
                {
                    int index = 0;
                    while (true)
                    {
                        DataPacket packet = new DataPacket(_buffer.Skip(index).Take(PACKET_SIZE).ToArray());
                        packets.Add(packet);
                        index += PACKET_SIZE;
                        if (read - index < PACKET_SIZE)
                        {
                            break;
                        }
                    }
                }

                foreach (DataPacket packet in packets)
                {
                    // for data packets indicating a change in relay or button status (relay on/off, button pressed/released)
                    // we invoke events that notify subscribers of these changes

                    switch (packet.Command)
                    {
                        case Command.RelayStatus:
                            byte relayState = _relayState;
                            var relayStatus = BuildRelayStatus(packet);
                            for (int i = 0; i < 8; i++)
                            {
                                bool relayOnOffState = relayStatus[i].CurrentlyOn;

                                if (relayState.GetBit(i) != relayOnOffState)
                                {
                                    _relayState = _relayState.SetBit(i, relayOnOffState);
                                    OnRelayStateChanged?.Invoke(this, relayStatus[i]);

                                    if (_timerActive.GetBit(i))
                                    {
                                        if (relayOnOffState == false)
                                        {
                                            _timerActive = _timerActive.SetBit(i, false);
                                            OnRelayTimerExpired?.Invoke(this, relayStatus[i]);
                                        }
                                        else
                                        {
                                            OnRelayTimerStarted?.Invoke(this, relayStatus[i]);  
                                        }
                                    }
                                }
                            }
                            break;
                        case Command.ButtonStatus:
                            var buttonStatus = BuildButtonStatus(packet);
                            for (int i = 0; i < 8; i++)
                            {
                                OnButtonStateChanged?.Invoke(this, buttonStatus[i]);      
                            }
                            break;
                    }
                }

                _packetsReceived = packets;
                _responseReceived = true; // this is a semaphore which allows methods that send a command and await a response to be completed (see AwaitAllResponses)
            }
        }

        private byte MaskFor(params int[] relayIndexes)
        {
            byte mask = 0;
            foreach (int index in relayIndexes)
            {
                if (index >= 0 && index < 8)
                {
                    mask = (byte)(mask | (1 << index));
                }
            }
            return mask;
        }

        private void EnsureConnected()
        {
            if (!Connected)
            {
                throw new NotConnectedException("No K8090 board is connected.");
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            _serialPort?.Dispose();
        }

        public void Finalize()
        {
            Dispose();
        }

        public RelayCard(string portName, ISerialPortStream portStream)
        {
            _serialPort = portStream;
            _serialPort.PortName = portName;
            _serialPort.Handshake = Handshake.None;
            _serialPort.DataReceived += DataReceived;
            _serialPort.ErrorReceived += ErrorReceived;
        }

        public RelayCard(string portName)
        {
            _serialPort = new SerialPortStream(portName, 19200, 8, Parity.None, StopBits.One);
            _serialPort.Handshake = Handshake.None;
            _serialPort.DataReceived += DataReceived;
            _serialPort.ErrorReceived += ErrorReceived;
        }
    }
}
