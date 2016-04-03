using System;
using Windows.Devices.Gpio;

namespace Robotics.Micro.Devices
{
    public class DigitalInputPin : Block
    {
        public Port Output { get; private set; }

        GpioPin pin;

        public DigitalInputPin (int pinNumber, bool glitchFilter = false)
		{
            Output = AddPort("Output", Units.Digital, 0.0);

            var pinc = GpioController.GetDefault();
            if (pinc == null)
            {
                Error("No Default GPIO Controller");
            }
            else {
                pin = pinc.OpenPin(pinNumber);
                pin.SetDriveMode(GpioPinDriveMode.Input);

                pin.ValueChanged += (s, e) =>
                {
                    Output.Value = pin.Read() == GpioPinValue.High ? 1.0 : 0.0;
                };

                Output.Value = pin.Read() == GpioPinValue.High ? 1.0 : 0.0;
            }
		}
    }
}
