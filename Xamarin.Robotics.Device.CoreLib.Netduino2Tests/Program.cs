using System;
using System.Threading;

using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.Netduino;

using Xamarin.Robotics.Device;
using Xamarin.Robotics.Sensors.Location;
using Xamarin.Robotics.Sensors.Motion;
using Xamarin.Robotics.Sensors.Temperature;
using Xamarin.Robotics.SpecializedBlocks;

namespace Xamarin.Robotics
{
	public class Program
	{
		public static void Main()
		{
            var scope = new DebugScope ();
            //scope.UpdatePeriod.Value = 1;

            var accel = new Memsic2125 ();
            accel.XPwmInput.ConnectTo (new DigitalInputPin (Pins.GPIO_PIN_D6).Output);
            accel.YPwmInput.ConnectTo (new DigitalInputPin (Pins.GPIO_PIN_D7).Output);
            scope.Connect (accel.XAccelerationOutput);
            scope.Connect (accel.YAccelerationOutput);

            var compass = new Grove3AxisDigitalCompass ();
            scope.Connect (compass.XGaussOutput);
            scope.Connect (compass.YGaussOutput);
            scope.Connect (compass.ZGaussOutput);

            var a0 = new AnalogInputPin (AnalogChannels.ANALOG_PIN_A0);
            scope.Connect (a0.Analog);

            var therm = new Thermistor ();
            therm.AnalogInput.ConnectTo (a0.Analog);
            scope.Connect (therm.Temperature);

            var b = new CelsiusToFahrenheit ();
            therm.Temperature.ConnectTo (b.Celsius);
            scope.Connect (b.Fahrenheit);


            var bmp = new Bmp085 ();
            scope.Connect (bmp.Temperature);

            var b2 = new CelsiusToFahrenheit ();
            bmp.Temperature.ConnectTo (b2.Celsius);
            scope.Connect (b2.Fahrenheit);


            for (; ; ) {
                Debug.Print ("Tick");
                Thread.Sleep (1000);
            }

		}
	}
}
