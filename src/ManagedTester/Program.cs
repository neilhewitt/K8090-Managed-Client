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

            if (args.Length == 0)
            {
                Console.WriteLine("No argument supplied. Argument must the COM port name to be used.");
                return;
            }

            Console.WriteLine("K8090 Board Test Program\n------------------------\n");
            Console.WriteLine(("Press 'q' to quit.\n"));

            try
            {
                /*** NOTE THAT YOU MUST SET THE CORRECT COM PORT NAME IN THE DEBUG -> COMMANDLINE SETTINGS BEFORE DEBUGGING THIS CODE. DEFAULT IS COM4 ***/

                using (RelayCard card = new(args[0]))
                {
                    card.OnRelayStateChanged += OnRelayStateChanged; // will display all relay change event data
                    card.OnRelayTimerStarted += OnRelayTimerStarted;
                    card.OnRelayTimerExpired += OnRelayTimerExpired;
                    card.Connect();
                    card.Reset();

                    card.SetAndStartRelayTimers(5, 1);

                    while (true)
                    {
                        // throb relay 0 on and off

                        card.SetRelayOn(0);
                        Thread.Sleep(500);
                        card.SetRelayOff(0);
                        Thread.Sleep(500);

                        if (Console.KeyAvailable)
                        {
                            if (Console.ReadKey(true).KeyChar == 'q')
                                return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void OnRelayTimerStarted(object sender, RelayStatus status)
        {
            Console.WriteLine($"Relay { status.RelayIndex } timer started.");
        }

        private static void OnRelayTimerExpired(object sender, RelayStatus status)
        {
            Console.WriteLine($"Relay { status.RelayIndex } timer expired.");
        }

        private static void OnRelayStateChanged(object sender, RelayStatus status)
        {
            Console.WriteLine($"Relay { status.RelayIndex } changed state to { (status.CurrentlyOn ? "ON" : "OFF") }");
        }
    }
}
