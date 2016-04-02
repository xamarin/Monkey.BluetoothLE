using System;
using Windows.Devices.Adc;
using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Devices
{
    public sealed class AnalogInputPin : PollingBlock
    {
        /// <summary>
        /// The analog value read on this pin. Values range from 0 to 1 (ratio).
        /// </summary>
        public Port Analog { get; private set; }

        AdcChannel input;

        public AnalogInputPin (int channelNumber, double updateFrequency = DefaultUpdateFrequency)
            : base (updateFrequency)
		{
            var inputc = AdcController.GetDefaultAsync().GetResults();
            input = inputc.OpenChannel (channelNumber);

            var initialValue = input.ReadRatio ();
            
            Analog = AddPort ("Analog", Units.Ratio, initialValue);

            StartPolling();
        }

        protected override void Poll ()
        {
            Analog.Value = input.ReadRatio ();
        }
    }
}
