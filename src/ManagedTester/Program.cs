using System;
using K8090.ManagedClient;
using K8090.ManagedClient.Mocks;
using RJCP.IO.Ports;
using System.Threading;
using System.Collections.Generic;

namespace ManagedTester
{
    class Program
    {
        static void Main(string[] args)
        {
            // this is just a simple test routine for the card - you can add extra tests to this
            // it's not a substitute for a unit test suite which is coming... honest!

            try
            {
                RelayCard card = new("COM4", new MockSerialPortStream());
                card.OnRelayStateChanged += OnRelayStateChanged; // will display all relay change event data
                card.Connect();
                card.Reset();

                while (true)
                {
                    // throb relay 0 on and off

                    card.SetRelayOn(0);
                    Thread.Sleep(500);
                    card.SetRelayOff(0);
                    Thread.Sleep(500);
                }

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void OnRelayStateChanged(object sender, RelayStatus status)
        {
            Console.WriteLine($"Relay { status.RelayIndex } changed state to { (status.CurrentlyOn ? "ON" : "OFF") }");
        }
    }
}
