using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RJCP.IO.Ports;
using K8090.ManagedClient;

namespace K8090.ManagedClient.Mocks
{
    public class MockRelayCard : RelayCard
    {
        private MockSerialPortStream _mockSerialPort;

        public event EventHandler OnSimulateButtonPress;
        public event EventHandler OnSimulateButtonRelease;

        public void SimulateButtonPress(int buttonIndex, TimeSpan holdFor)
        {
            _mockSerialPort.SimulateButtonPress(buttonIndex, holdFor);
        }

        public MockRelayCard(string comPort, bool makeDelaySecondsIntoMilliseconds) 
            : base(comPort, new MockSerialPortStream(makeDelaySecondsIntoMilliseconds))
        {
            _mockSerialPort = _serialPort as MockSerialPortStream;
            _mockSerialPort.OnSimulatedButtonPress += (sender, e) => OnSimulateButtonPress?.Invoke(this, e);
            _mockSerialPort.OnSimulatedButtonRelease += (sender, e) => OnSimulateButtonRelease?.Invoke(this, e);
        }
    }
}
