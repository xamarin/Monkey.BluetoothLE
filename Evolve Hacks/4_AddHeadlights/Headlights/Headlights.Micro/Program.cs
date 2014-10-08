using System;
using System.IO.Ports;
using Robotics.Messaging;
using Robotics.Micro.Devices;
using SecretLabs.NETMF.Hardware.Netduino;

namespace Robotics.Micro.Core.Netduino2Tests
{
	public class TestHeadLights
	{
		public static void Main ()
		{
			// Create the blocks
			var leftLight = new DigitalOutputPin (Pins.GPIO_PIN_D10) { Name = "LeftLight" };
			var rightLight = new DigitalOutputPin (Pins.GPIO_PIN_D11) { Name = "RightLight" };

			// Init
			leftLight.Input.Value = 0;
			rightLight.Input.Value = 0;

			// Create the control server
			var serialPort = new SerialPort (SerialPorts.COM3, 57600, Parity.None, 8, StopBits.One);
			serialPort.Open ();
			var server = new ControlServer (serialPort);

			// Expose the left and right lights to the control server
			leftLight.Input.ConnectTo (server, writeable: true);
			rightLight.Input.ConnectTo (server, writeable: true);

			// Do nothing
			for (; ; ) {
				System.Threading.Thread.Sleep (1000);
			}
		}
	}
}