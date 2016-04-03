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
                if (glitchFilter)
                {
                    pin.DebounceTimeout = TimeSpan.FromMilliseconds(10);
                }

                pin.ValueChanged += Pin_ValueChanged;

                Output.Value = pin.Read() == GpioPinValue.High ? 1.0 : 0.0;
            }
		}

        public void Stop ()
        {
            var p = pin;
            pin = null;

            if (p != null)
            {
                p.ValueChanged -= Pin_ValueChanged;
                p.Dispose();
            }
        }

        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (pin != null)
            {
                Output.Value = pin.Read() == GpioPinValue.High ? 1.0 : 0.0;
            }
        }
    }
}
