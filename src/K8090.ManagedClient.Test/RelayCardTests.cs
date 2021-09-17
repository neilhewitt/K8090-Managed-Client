using System;
using System.Collections.Generic;
using K8090.ManagedClient;
using K8090.ManagedClient.Mocks;
using NUnit.Framework;

namespace K8090.ManagedClient.Test.Unit
{
    [TestFixture]
    public class RelayCardTests
    {
        private RelayCard _card;

        [SetUp]
        public void Setup()
        {
            _card = new RelayCard("COM4", new MockSerialPortStream());
            _card.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _card.Disconnect();
            _card.Dispose();
        }

        // actual tests coming soon...
    }
}
