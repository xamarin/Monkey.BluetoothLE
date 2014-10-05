using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using H = Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using Robotics.Micro;
using Robotics.Micro.Devices;

namespace ButtonPush
{
	public class Program
	{
		// create our pin references.
		// note, when you create a reference to the onboard button, netduino is 
		// smart enough to not use it as a reset button. :)
		static H.Cpu.Pin buttonHardware = Pins.ONBOARD_BTN;
		static H.Cpu.Pin ledHardware = Pins.ONBOARD_LED;

		public static void Main()
		{
			// Create the blocks
			var button = new DigitalInputPin(buttonHardware);
			var led = new DigitalOutputPin(ledHardware);

			// Connect them together. with the block/scope architecture, you can think
			// of everything as being connectable - output from one thing can be piped
			// into another. in this case, we're setting the button output to the LED
			// input. so when the user presses on the button, the signal goes straight
			// to the LED.
			button.Output.ConnectTo(led.Input);

			// keep the main loop alive so the program doesn't exit.
			while (true)
			{
				Thread.Sleep(1000);
			}
		}
	}
}
