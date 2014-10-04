using System;
using Microsoft.SPOT;

namespace Robotics.Micro.Generators
{
    public class SineWave : Generator
    {
        public InputPort FrequencyInput { get; private set; }
        public InputPort AmplitudeInput { get; private set; }
        public InputPort OffsetInput { get; private set; }

        public SineWave (double frequency = 1, double amplitude = 1, double offset = 0, double updateFrequency = DefaultUpdateFrequency)
            : base (updateFrequency)
        {
            FrequencyInput = AddInput ("FrequencyInput", Units.Frequency, frequency);
            AmplitudeInput = AddInput ("AmplitudeInput", Units.Scalar, amplitude);
            OffsetInput = AddInput ("OffsetInput", Units.Scalar, offset);
        }

        protected override double Generate (double time)
        {
            // Debug.Print ("Generate at t = " + time);

            var angle = time * 2 * System.Math.PI * FrequencyInput.Value;
            return AmplitudeInput.Value * System.Math.Sin (angle) + OffsetInput.Value;
        }
    }
}
