using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.Threading;
using Robotics.Micro.Devices;
using Robotics.Micro.Sensors.Location;
using Robotics.Micro.Sensors.Motion;
using Robotics.Micro.Sensors.Proximity;
using Robotics.Micro.Sensors.Temperature;
using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Core.Netduino2Tests
{
    public class TestSensors
    {
        public static void Run ()
        {
            var scope = new DebugScope ();
            //scope.UpdatePeriod.Value = 1;

            var accel = new Memsic2125 ();
            accel.XPwmInput.ConnectTo (new DigitalInputPin (Pins.GPIO_PIN_D6).Output);
            accel.YPwmInput.ConnectTo (new DigitalInputPin (Pins.GPIO_PIN_D7).Output);
            scope.ConnectTo (accel.XAccelerationOutput);
            scope.ConnectTo (accel.YAccelerationOutput);

            var compass = new Grove3AxisDigitalCompass ();
            scope.ConnectTo (compass.XGaussOutput);
            scope.ConnectTo (compass.YGaussOutput);
            scope.ConnectTo (compass.ZGaussOutput);

            var a0 = new AnalogInputPin (AnalogChannels.ANALOG_PIN_A0);
            scope.ConnectTo (a0.Analog);

            var sharp = new SharpGP2D12 ();
            a0.Analog.ConnectTo (sharp.AnalogInput);
            scope.ConnectTo (sharp.DistanceOutput);

            var therm = new Thermistor ();
            therm.AnalogInput.ConnectTo (a0.Analog);
            scope.ConnectTo (therm.Temperature);

            var b = new CelsiusToFahrenheit ();
            therm.Temperature.ConnectTo (b.Celsius);
            scope.ConnectTo (b.Fahrenheit);


            var bmp = new Bmp085 ();
            scope.ConnectTo (bmp.Temperature);

            var b2 = new CelsiusToFahrenheit ();
            bmp.Temperature.ConnectTo (b2.Celsius);
            scope.ConnectTo (b2.Fahrenheit);


            for (; ; ) {
                Debug.Print ("Tick");
                Thread.Sleep (1000);
            }
        }

    }
}
