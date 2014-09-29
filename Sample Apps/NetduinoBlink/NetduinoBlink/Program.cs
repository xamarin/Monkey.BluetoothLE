using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using SecretLabs.NETMF.Hardware.Netduino;

namespace NetduinoBlink
{
	public class Program
	{
		public static void Main()
		{
			// configure an output port for us to "write" to the LED
			OutputPort led = new OutputPort(Pins.ONBOARD_LED, false); 
			// note that if we didn't have the SecretLabs.NETMF.Hardware.Netduino DLL, we could also manually access it this way:
			//OutputPort led = new OutputPort(Cpu.Pin.GPIO_Pin10, false); 
			int i = 0;
			while (true) 
			{ 
				led.Write(true); // turn on the LED 
				Thread.Sleep(250); // sleep for 250ms 
				led.Write(false); // turn off the LED 
				Thread.Sleep(250); // sleep for 250ms 

				Debug.Print ("Looping" + i);
				i++;
			} 						
		}
	}
}
