using System;
using System.Collections.Generic;
using System.Threading;
using K8090.ManagedClient;
using K8090.ManagedClient.Mocks;
using NUnit.Framework;

namespace K8090.ManagedClient.Test.Unit
{
    [TestFixture]
    public class RelayCardTests
    {
        private MockRelayCard _card;

        [SetUp]
        public void Setup()
        {
            _card = new MockRelayCard("COM4", true);
            _card.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _card.Disconnect();
            _card.Dispose();
        }

        [Test]
        public void WhenNotConnectedAndCommandSentNotConnectedExceptionIsThrown()
        {
            _card.Disconnect();

            Assert.Throws<NotConnectedException>(() =>
              _card.GetRelayStatus()
            );
        }

        [Test]
        public void WhenSetRelayOnCalledRelayIndicatesOn()
        {
            _card.SetRelayOn(0);
            Assert.That(_card.RelayState[0] == true);
        }

        [Test]
        public void WhenSetRelayOnCalledRelayStateEventIsRecieved()
        {
            RelayStatus relayStatus = null;
            _card.OnRelayStateChanged += (sender, status) => relayStatus = status;

            _card.SetRelayOn(0);
            Assert.That(relayStatus, Is.Not.Null);
            Assert.That(relayStatus?.CurrentlyOn, Is.True);
        }

        [Test]
        public void WhenSetRelaysOnCalledSpecifiedRelaysIndicateOn()
        {
            _card.SetRelaysOn(0, 2, 4, 6);
            Assert.That(_card.RelayState, Is.EqualTo(new bool[] { true, false, true, false, true, false, true, false }));
        }

        [Test]
        public void WhenGetRelayStatusCalledCorrectDataReturned()
        {
            _card.SetRelaysOn(0, 2, 4, 6);
            IDictionary<int, RelayStatus> state = _card.GetRelayStatus();

            Assert.That(state.Count, Is.EqualTo(8));
            for(int i = 0; i < 8; i+=2)
            {
                Assert.That(state[i].CurrentlyOn, Is.EqualTo(true));
            }
        }

        [Test]
        public void WhenGetButtonModesCalledCorrectDataReturned()
        {
            _card.SetButtonModes(ButtonMode.Momentary);
            IDictionary<int, ButtonMode> state = _card.GetButtonModes();

            Assert.That(state.Count, Is.EqualTo(8));
            for (int i = 0; i < 8; i++)
            {
                Assert.That(state[i], Is.EqualTo(ButtonMode.Momentary));
            }
        }

        [Test]
        public void WhenCustomTimerSetRelayTurnsOnAndOff()
        {
            ushort delay = 10; // in ms, because we set 'timersInMilliseconds' true - on the real board, timers are in seconds only
            _card.SetAndStartRelayTimers(delay, 0);

            Assert.That(_card.RelayState[0], Is.True);
            Thread.Sleep(100); // need to wait to account for the mock protocol run - on first run the compile can slow this down so much the test would fail
            Assert.That(_card.RelayState[0], Is.False);
        }

        [Test]
        public void WhenDefaultTimerSetRelayTurnsOnAndOff()
        {
            _card.StartRelayTimers(0);

            Assert.That(_card.RelayState[0], Is.True);
            Thread.Sleep(100); // need to wait to account for the mock protocol run
            Assert.That(_card.RelayState[0], Is.False);
        }
    }
}
