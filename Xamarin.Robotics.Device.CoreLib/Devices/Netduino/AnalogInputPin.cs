using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Xamarin.Robotics.SpecializedBlocks;

namespace Xamarin.Robotics.Device
{
    public class AnalogInputPin : PollingBlock
    {
        /// <summary>
        /// The analog value read on this pin. Values range from 0 to 1 (ratio).
        /// </summary>
        public Port Analog { get; private set; }

        AnalogInput input;

        public AnalogInputPin (Cpu.AnalogChannel pin)
		{
            input = new AnalogInput (pin, -1);

            var initialValue = input.Read ();
            
            Analog = AddPort ("Analog", Units.Ratio, initialValue);
        }

        protected override void Poll ()
        {
            Analog.Value = input.Read ();
        }
    }
}
