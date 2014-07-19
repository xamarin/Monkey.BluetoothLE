using System;
using Microsoft.SPOT;

namespace Xamarin.Robotics.SpecializedBlocks
{
    public class SineWaveGenerator : Generator
    {
        public InputPort FrequencyInput { get; private set; }
        public InputPort AmplitudeInput { get; private set; }
        public InputPort OffsetInput { get; private set; }

        public SineWaveGenerator (double frequency = 1, double amplitude = 1, double offset = 0, double updateFrequency = DefaultUpdateFrequency)
            : base (updateFrequency)
        {
            FrequencyInput = new InputPort (this, "FrequencyInput", Units.Frequency, frequency);
            AmplitudeInput = new InputPort (this, "AmplitudeInput", Units.Scalar, amplitude);
            OffsetInput = new InputPort (this, "OffsetInput", Units.Scalar, offset);
        }

        protected override double Generate (double time)
        {
            Debug.Print ("Generate at t = " + time);

            var angle = time * 2 * System.Math.PI * FrequencyInput.Value;
            return AmplitudeInput.Value * System.Math.Sin (angle) + OffsetInput.Value;
        }
    }
}
