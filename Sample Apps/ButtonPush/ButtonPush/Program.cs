using System;
using Microsoft.SPOT;
using H = Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.Threading;
using Robotics.Micro;
using Robotics.Micro.Sensors.Buttons;

namespace ButtonPush
{
	public class Program
	{
		public static void Main()
		{
            PushButton button = new PushButton();
            //button.ConnectTo(new DigitalInputPin(Pins.ONBOARD_BTN).Output);
            InputPort buttonIn = new InputPort(button, "MyButton", Units.Digital);
			//buttonIn.ConnectTo(Pins.ONBOARD_BTN);
            H.OutputPort led = new H.OutputPort(Pins.ONBOARD_LED, false);

			bool _isLedOn = false;

            button.Clicked += (s, e) => {
				_isLedOn = !_isLedOn;
				led.Write(_isLedOn);
            };


			while (true) 
			{ 
			} 			
		}

        static void button_Clicked(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
	}
}
