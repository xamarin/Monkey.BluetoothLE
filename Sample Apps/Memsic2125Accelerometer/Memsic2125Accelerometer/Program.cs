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
using Robotics.Micro.Sensors.Motion;

namespace Memsic2125Accelerometer
{
	/// <summary>
	/// This application illustrates how to use a Memsic 2125 Accelerometer. Wire 
	/// the YOut (Left center pin) to digital pin 12 and XOut (Right center pin) to 
	/// digital pin 11:
	/// http://www.ee.ryerson.ca/~jkoch/courses/ele604/Data/28017-Memsic2Axis-v2.0.pdf
	/// </summary>
	public class Program
	{
		public static void Main()
		{
			// A debug scope allows us to listen to a block (in this case the 
			// accelerometer), and automatically write its output to the console.
			DebugScope scope = new DebugScope ();
			scope.UpdatePeriod.Value = .5; // update 2x/second

			// create a new instance of the Memsic2125 class
			Memsic2125 accelerometer = new Memsic2125 ();
			// connect the acceleromter outputs to the output through pins 11 and 12
			accelerometer.XPwmInput.ConnectTo (new DigitalInputPin (Pins.GPIO_PIN_D11).Output);
			accelerometer.YPwmInput.ConnectTo (new DigitalInputPin (Pins.GPIO_PIN_D12).Output);
			// connect our scope the acceleromter output
			scope.ConnectTo (accelerometer.XAccelerationOutput);
			scope.ConnectTo (accelerometer.YAccelerationOutput);

			// keep the program alive
			while (true)
			{
				Thread.Sleep(1000);
				Debug.Print("Waiting a second.");
			}
		}

	}
}
