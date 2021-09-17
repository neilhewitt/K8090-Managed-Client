using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace K8090.ManagedClient.Mocks
{
    public class MockRelayProtocol
    {
        private MockSerialPortStream _serialPort;

        private ushort[] _timerDelays = new ushort[] { 5, 5, 5, 5, 5, 5, 5, 5 };
        private IDictionary<int, Timer> _timers = new Dictionary<int, Timer>();
        private bool[] _timersActive = new bool[8]; 
        private bool[] _relays = new bool[8]; 
        private ButtonMode[] _buttonModes = new ButtonMode[8];
        private bool _eventJumper = false;

        public void PressButton(int buttonIndex, TimeSpan holdFor)
        {
            switch(_buttonModes[buttonIndex])
            {
                case ButtonMode.Toggle:
                    if (!_eventJumper) SetRelays((byte)buttonIndex, !_relays[buttonIndex]);
                    SendButtonStatus(buttonIndex, true);
                    System.Threading.Thread.Sleep(holdFor.Milliseconds);
                    SendButtonStatus(buttonIndex, false);
                    break;
                case ButtonMode.Momentary:
                    if (!_eventJumper) SetRelays((byte)buttonIndex, true);
                    SendButtonStatus(buttonIndex, true);
                    System.Threading.Thread.Sleep(holdFor.Milliseconds);
                    if (!_eventJumper) SetRelays((byte)buttonIndex, false);
                    SendButtonStatus(buttonIndex, false);
                    break;
                case ButtonMode.Timer:
                    SendButtonStatus(buttonIndex, true);
                    if (!_eventJumper)
                    {
                        if (_timersActive[buttonIndex])
                        {
                            _timers[buttonIndex].Stop();
                            SetRelays((byte)buttonIndex, false);
                        }
                        else
                        {
                            ActivateTimers((byte)buttonIndex, 0, 0);
                        }
                    }
                    System.Threading.Thread.Sleep(holdFor.Milliseconds);
                    SendButtonStatus(buttonIndex, false);
                    break;
            }
        }

        public byte[] GetResponseFor(byte[] request)
        {
            if (request.Length != 7) throw new ArgumentException("Request length must be 7 bytes");

            Command command = (Command)request[1];
            byte mask = request[2];
            byte param1 = request[3];
            byte param2 = request[4];   
            switch (command)
            {
                case Command.RelayOn:
                    SetRelays(mask, true);
                    break;
                case Command.RelayOff:
                    SetRelays(mask, false);
                    break;
                case Command.RelayToggle:
                    byte state = 0;
                    for (int i = 0; i < 8; i++) state = SetBit(state, i, !_relays[i]);
                    SetRelays(mask, state);
                    break;
                case Command.SetButtonMode:
                    for (int i = 0; i < 8; i++)
                    {
                        if (GetBit(mask, i)) _buttonModes[i] = ButtonMode.Momentary;
                        if (GetBit(param1, i)) _buttonModes[i] = ButtonMode.Toggle;
                        if (GetBit(param2, i)) _buttonModes[i] = ButtonMode.Timer;
                    }
                    break;
                case Command.StartRelayTimer:
                    ActivateTimers(mask, param1, param2);
                    SetRelays(mask, true);
                    break;
                case Command.SetRelayTimerDelay:
                    for (int i = 0; i < 8; i++)
                    {
                        if (GetBit(mask, i))
                        {
                            ushort delay = (ushort)((param1 * 256) + param2);
                            if (delay > 0)
                            {
                                _timerDelays[i] = delay;
                            }
                        }
                    }
                    break;
                case Command.ResetFactoryDefaults:
                    break;
                case Command.QueryRelayState:
                case Command.RelayStatus:
                    byte b1 = 0, b2 = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (_relays[i]) b1 = SetBit(b1, i, true);
                        if (_timersActive[i]) b2 = SetBit(b2, i, true);
                    }
                    return MakeData(Response.RelayStatus, 0x00, b1, b2);
                case Command.QueryRelayTimerDelay:
                    List<byte> bytes = new();
                    for (int i = 0; i < 8; i++)
                    {
                        mask = 0;
                        mask = SetBit(mask, i, true);
                        bytes.AddRange(MakeData(Response.QueryRelayTimerDelay, mask, (byte)(_timerDelays[i] / 256), (byte)(_timerDelays[i] % 256)));
                    }
                    return bytes.ToArray();
                case Command.ButtonStatus:
                    return MakeData(Response.ButtonStatus);
                case Command.JumperStatus:
                    return MakeData(Response.JumperStatus);
                case Command.FirmwareVersion:
                    return MakeData(Response.FirmwareVersion, 0, 10, 1);
            }

            return null;
        }

        private void SetRelays(byte mask, bool on)
        {
            byte state = 0;
            for (int i = 0; i < 8; i++)
            {
                if (GetBit(mask, i)) state = SetBit(state, i, on);
            }
            
            SetRelays(mask, state);
        }

        private void SetRelays(byte mask, byte state)
        {
            for (int i = 0; i < 8; i++)
            {
                if (GetBit(mask, i)) _relays[i] = GetBit(state, i);
            }

            SendRelayStatus();
        }

        private void SendRelayStatus()
        {
            byte mask = 0;
            byte param1 = 0;
            byte param2 = 0;

            for (int i = 0; i < 8; i++)
            {
                if (_relays[i]) param1 = SetBit(param1, i, true);
                if (_timersActive[i]) param2 = SetBit(param2, i, true);
            }

            _serialPort.InvokeDataReceived(MakeData(Response.RelayStatus,mask, param1, param2));
        }

        private void SendButtonStatus(int buttonIndex, bool pressed)
        {
            byte mask = 0;
            byte param1 = 0;
            byte param2 = 0;

            _serialPort.InvokeDataReceived(MakeData(
                Response.ButtonStatus,
                mask = SetBit(mask, buttonIndex, pressed),
                param1 = SetBit(param1, buttonIndex, pressed),
                param2 = SetBit(param2, buttonIndex, !pressed)
                ));
        }

        private void ActivateTimers(byte mask, byte param1, byte param2)
        {
            for (int i = 0; i < 8; i++)
            {
                if (GetBit(mask, i))
                {
                    Timer timer = _timers[i];
                    if (timer.Enabled) timer.Stop();
                    int delay = (param1 * 256) + param2;
                    if (delay == 0) delay = _timerDelays[i];
                    timer.Interval = delay * 1000;
                    _timersActive[i] = true;
                    timer.Start();
                }
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Timer timer = (Timer)sender;
            timer.Stop();
            int index = _timers.Single(x => x.Value == timer).Key;
            _timersActive[index] = false;
            byte mask = 0;
            mask = SetBit(mask, index, true);
            SetRelays(mask, false);
        }

        private byte[] MakeData(Response response, byte mask = 0, byte param1 = 0, byte param2 = 0)
        {
            byte checksum = (byte)((-(0x04 + (byte)response + mask + param1 + param2)));
            return new byte[] { 0x04, (byte)response, mask, param1, param2, checksum, 0x0F };
        }

        private bool GetBit(byte input, int bitIndex)
        {
            return (input & (1 << bitIndex)) != 0;
        }

        private byte SetBit(byte input, int bitIndex, bool state)
        {
            return state switch
            {
                true => (byte)(input | (1 << bitIndex)),
                false => (byte)(input & ~(1 << bitIndex))
            };
        }

        public MockRelayProtocol(MockSerialPortStream serialPort)
        {
            _serialPort = serialPort;
            for(int i = 0; i < 8; i++)
            {
                _timers.Add(i, new System.Timers.Timer());
                _timers[i].Elapsed += Timer_Elapsed;
            }
        }
    }
}
