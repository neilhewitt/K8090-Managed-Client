# K8090 Managed Client Library

A 64-bit managed client library for the Velleman K8090/VM8090/WMI8090 USB relay boards. 

Since Velleman has not provided 64-bit versions of their .NET / COM libraries, I decided to write one myself. The project is built for .NET 5 / AnyCPU and it *should* work cross-platform but I have not tested on any OS other than Windows at this point. Technically it should work on a 32-bit platform too but again this is untested. Hopefully I'll get an opportunity to test these in the near future, but if you manage to get this library running successfully on Linux or MacOS please open an issue to let me know or send a PR if you needed to fix the code. 

_Note that if you want to run this on Linux or MacOS there are some support libraries that must be installed for the underlying serial port software to work and these are not part of this project or any NuGet package. See the notes here: https://github.com/jcurl/serialportstream._

The client talks directly to the serial port to which the relay board is assigned. Commands are sent and events received via 7-byte data packets. The client wraps this process and provides suitably abstracted methods to access the various functions of the board.

## Getting started ##

    using K8090.ManagedClient;
    
    ...

    RelayCard card = new RelayCard("COM4");
    card.Connect();

    card.Reset();
    for (int i = 0; i < 8; i++)
    {
        card.SetRelayOn(i); // turn on relay number i
        Thread.Sleep(1000);
        card.SetRelayOff(i); // turn the relay off again
    }
  
This code connects to a K8090 board on COM4, sets all relays to OFF, then loops through each relay, turns it on for 1 second, and then off again. 

It's pretty simple!


## Mocking for development or testing without the board ##

If you're looking to develop code using the library but you don't have a K8090 available yet, or you want to use it in code you will be unit testing, you can still use the library by using a mock. Include the _K8090.ManagedClient.Mocks_ assembly and instead of ***RelayCard*** use:

    MockRelayCard card = new MockRelayCard("COM4");

The ***MockSerialPortStream*** used in the ***MockRelayCard*** implements *ISerialPortStream* and behaves as if it were a real serial port connected to a real K8090 board. You can use the APIs of ***RelayCard*** and get the same results, in most cases, as you would with the actual hardware. 

With the ***MockRelayCard***, you can call *SimulateButtonPress(int buttonIndex, TimeSpan holdFor)* to simulate pressing one of the buttons on the board for a time span, and this will generate the correct actions and events as if the hardware button was pressed.

Note: When creating the ***MockRelayCard*** instance you can specify an optional boolean parameter in the constructor _makeDelaySecondsIntoMilliseconds_. If _true_, all simulated delays are changed from seconds (as on the actual board) to milliseconds. This is useful for test suites where you don't want to delay for a second or more. See the included test suite for an example of this approach. 


## Client API ##

* Connect(*string comPort*) - attempts to connect to a board on the COM port specified ie "COM4"
* Disconnect() - closes the connection
* Reset() - sets all relays on the board to OFF
* SetRelayOn(*int index*) - sets a relay (with index 0-7) to ON
* SetRelaysOn(*params int[] relayIndexes*) - sets a group of relays ON
* SetRelayOff(*int index*) - sets a relay to OFF
* SetRelaysOff(*params int[] relayIndexes*) - sets a group of relays OFF
* ToggleRelay(*int index*) - toggles the state of a relay
* ToggleRelays(*params int[] relayIndexes*) - toggles a group of relays
* GetRelayStatus() - returns an *IDictionary<int, RelayStatus>* giving the status of all relays (see **RelayStatus** below)
* GetButtonModes() - returns an *IDictionary<int, ButtonMode>* giving the mode of all buttons (see **ButtonMode** below)
* SetButtonModes(*ButtonMode mode*) - sets all buttons to the specified button mode
* SetButtonModes(*IDictionary<int, ButtonMode>*) - sets button modes for a set of buttons
* StartRelayTimers(*params int[] relayIndexes*) - starts the timers attached to a set of relays
* SetAndStartRelayTimers(*ushort delayInSeconds*, *params int[] relayIndexes*) - sets and then starts the timer for the specified relays
* SetRelayTimerDefaultDelay(*ushort delayInSeconds*, *params int[] relayIndexes*) - sets the default delay for the specified relays
* GetRelayTimerDelays(*params int[] relayIndexes*) - gets the timer delay for specified relays
* GetRelayTimerDelaysRemaining(*params int[] relayIndexes*) - gets the delay remaining for specified relays
* GetFirmwareVersion() - gets a string containing the firmware version of the board
* ResetFactoryDefaults() - resets the board to factory defaults
* IsEventJumperSet() - returns true if the event jumper is set, false if not

Many APIs follow the pattern of taking a _params int[]_ of relay indexes to be affected by the operation. Indexes run from 0-7. 

***SetButtonModes*** takes an *IDictionary<int, ButtonMode>* giving a specific button mode for the relay indicated by the integer in each dictionary pair.

If you specify an out of range index, that command will be ignored and/or null returned. Exceptions are not thrown for out of range indexes.


## Timers ##

Each relay has a timer with a value in seconds which can be triggered. When the timer is started, the relay is set to ON, and when it expires, the relay is set to OFF. Timer values are stored in EEPROM on the board, so if you change the default delay using ***SetRelayTimerDefaultDelay***, this delay is stored when the board is powered down and restored when it is powered up again. You can also start the timer for each relay with a custom delay which is not stored and does not affect the default delay by using ***SetAndStartRelayTimers***. You can query the delay attached to one or more relays with ***GetRelayTimerDelays***, and if you need to find out how much time is remaining on a timer that has been started, ***GetRelayTimerDelaysRemaining*** is the API you need.


## Events ##

The client exposes two events, ***OnRelayStateChanged*** and ***OnButtonStateChanged***. These are fired when the state of a relay or button changes (for example, when you give the command to set it OFF when it is ON, or when the button in Toggle mode is pressed) and provide a *RelayStatus* or *ButtonStatus* object for that relay or button. When multiple relays change state at the same time, this event will fire once for each relay. They do not fire for changes to *ButtonMode* or timer settings.

The client also exposes the events ***OnRelayTimerStarted*** and ***OnRelayTimerExpired***. These fire when a timer starts and expires respectively, and each supplies a *RelayStatus* for the relay whose timer has just started or expired. Note that the ***OnRelayStateChanged*** event will also fire for the same relay as well when the timer starts or expires.


## RelayStatus ##

The *RelayStatus* object gives details of the status of a relay, including:

* Its index number
* Whether it is currently set to ON
* Whether it was previously set to ON and has changed
* Whether its timer is currently active


## ButtonStatus ##

The *ButtonStatus* object gives details of the status of a button on the board, including:

* Its index number
* Whether is it currently pressed
* Whether it was previously pressed
* Whether it was previously released


## ButtonMode ##

Buttons can be set to one of three modes:

* Toggle - pressing the button toggles the state of the relay
* Momentary - pressing and holding the button toggles the relay ON, and releasing it toggles it OFF
* Timer - pressing the button toggles the relay ON and starts the timer


## The Event Jumper ##

If this jumper on the board is set to ON, then button presses do not affect the relays, but the ***OnButtonStateChange*** event is still fired. This is useful if you attach separate buttons to the button interface port on the board, so you can intercept a button press in software and react accordingly.


## Acknowledgements ##

This library uses SerialPortStream by Jason Curl (https://www.nuget.org/packages/SerialPortStream) which is an awesome drop-in replacement for the built-in SerialPort support from Microsoft. SerialPort has significant issues that SerialPortStream fixes.
