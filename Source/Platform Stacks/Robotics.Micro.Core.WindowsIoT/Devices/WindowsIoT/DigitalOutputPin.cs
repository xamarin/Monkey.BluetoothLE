using System;
using Windows.Devices.Gpio;

namespace Robotics.Micro.Devices
{
    public class DigitalOutputPin : Block
    {
        public InputPort Input { get; private set; }

        GpioPin pin;

        const double HighMinValue = 0.2;

        public DigitalOutputPin (int pinNumber, double initialValue = 0)
		{
            Input = AddInput ("Input", Units.Digital, initialValue);

            var pinc = GpioController.GetDefault();
            if (pinc == null)
            {
                Error("No Default GPIO Controller");
            }
            else {
                pin = pinc.OpenPin(pinNumber);
                pin.Write(initialValue >= HighMinValue ? GpioPinValue.High : GpioPinValue.Low);
                pin.SetDriveMode(GpioPinDriveMode.Output);

                Input.ValueChanged += (s, e) =>
                {
                    if (pin != null)
                    {
                        pin.Write(Input.Value >= HighMinValue ? GpioPinValue.High : GpioPinValue.Low);
                    }
                };
            }
		}

        public void Stop()
        {
            var p = pin;
            pin = null;

            if (p != null)
            {                
                p.Dispose();
            }
        }
    }
}
