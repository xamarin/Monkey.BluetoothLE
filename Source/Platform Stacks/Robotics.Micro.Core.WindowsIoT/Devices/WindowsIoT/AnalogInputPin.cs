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
            Analog = AddPort("Analog", Units.Ratio, 0.0);

            var inputc = AdcController.GetDefaultAsync().AsTask().Result;
            if (inputc == null)
            {
                Error("No Default ADC Controller");
            }
            else {
                input = inputc.OpenChannel(channelNumber);

                var initialValue = input.ReadRatio();

                Analog.Value = initialValue;

                StartPolling();
            }
        }

        public void Stop ()
        {
            var i = input;
            input = null;

            if (i != null)
            {
                i.Dispose();
            }

            StopPolling();
        }

        protected override void Poll ()
        {
            if (input != null)
            {
                Analog.Value = input.ReadRatio();
            }
        }
    }
}
