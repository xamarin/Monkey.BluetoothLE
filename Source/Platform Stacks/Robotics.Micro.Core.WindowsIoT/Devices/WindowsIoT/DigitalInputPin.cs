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
            pin = GpioController.GetDefault().OpenPin(pinNumber);
            pin.SetDriveMode(GpioPinDriveMode.Input);

            var initialValue = pin.Read () == GpioPinValue.High ? 1.0 : 0.0;
            
            Output = AddPort ("Output", Units.Digital, initialValue);

            pin.ValueChanged += (s, e) => {
                Output.Value = pin.Read() == GpioPinValue.High ? 1.0 : 0.0;
            };
		}
    }
}
