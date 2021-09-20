using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using K8090.ManagedClient;
using K8090.ManagedClient.Mocks;
using NUnit.Framework;

namespace K8090.ManagedClient.Test.Unit
{
    [TestFixture]
    public class MockRelayProtocolTests
    {
        public const int TEST_BUTTON = 0;

        private MockRelayCard _card;
        private ButtonStatus _buttonStatus;
        private RelayStatus _relayStatus; 

        [SetUp]
        public void Setup()
        {
            _card = new MockRelayCard("COM4", true);
            
            _card.OnButtonStateChanged += (sender, status) => {
                if (status.ButtonIndex == TEST_BUTTON) _buttonStatus = status;
            };

            _card.OnRelayStateChanged += (sender, status) => {
                if (status.RelayIndex == TEST_BUTTON) _relayStatus = status;
            };
            
            _card.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _card.Disconnect();
            _card.Dispose();
        }

        [Test]
        public void WhenButtonPressedAndJumperIsOnRelaysAreUnaffected()
        {
            _card.OnSimulateButtonPress += (sender, e) => {
                Assert.That(_relayStatus.CurrentlyOn, Is.False);
                Assert.That(_buttonStatus.PressedNow, Is.True);
            };

            _card.SimulateSetJumper(true);
            _card.SimulateButtonPress(0, TimeSpan.FromMilliseconds(10));
        }

        [Test]
        public void WhenMomentaryButtonPressedAndJumperIsOffStatusUpdatesCorrectly()
        {
            _card.OnSimulateButtonPress += (sender, e) => {
                Assert.That(_relayStatus.CurrentlyOn, Is.True);
                Assert.That(_buttonStatus.PressedNow, Is.True);
            };

            _card.OnSimulateButtonRelease += (sender, e) => {
                Assert.That(_relayStatus.CurrentlyOn, Is.False);
                Assert.That(_buttonStatus.PressedNow, Is.False);
            };

            _card.SetButtonModes(ButtonMode.Momentary);
            _card.SimulateButtonPress(TEST_BUTTON, TimeSpan.FromMilliseconds(10));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WhenToggleButtonPressedAndJumperIsOffStatusUpdatesCorrectly(bool relayWasPreviouslyOn)
        {
            if (relayWasPreviouslyOn) _card.SetRelayOn(0);

            _card.OnSimulateButtonPress += (sender, e) => {
                Assert.That(_relayStatus.CurrentlyOn, Is.EqualTo(!relayWasPreviouslyOn));
                Assert.That(_buttonStatus.PressedNow, Is.True);
            };

            _card.OnSimulateButtonRelease += (sender, e) => {
                Assert.That(_relayStatus.CurrentlyOn, Is.EqualTo(!relayWasPreviouslyOn));
                Assert.That(_buttonStatus.PressedNow, Is.False);
            };

            _card.SetButtonModes(ButtonMode.Toggle);
            _card.SimulateButtonPress(TEST_BUTTON, TimeSpan.FromMilliseconds(10));
        }

        [Test]
        public void WhenTimerButtonPressedAndJumperIsOffStatusUpdatesCorrectly()
        {
            _card.OnSimulateButtonPress += (sender, e) => {
                Assert.That(_relayStatus.CurrentlyOn, Is.True);
                Assert.That(_relayStatus.TimerActive, Is.True);
                Assert.That(_buttonStatus.PressedNow, Is.True);
            };

            _card.OnSimulateButtonRelease += (sender, e) => {
                Assert.That(_buttonStatus.PressedNow, Is.False);
            };

            _card.SetRelayTimerDefaultDelay(10, 0); // time is actually in ms
            _card.SetButtonModes(ButtonMode.Timer);

            _card.SimulateButtonPress(TEST_BUTTON, TimeSpan.FromMilliseconds(10));
        }
    }
}
