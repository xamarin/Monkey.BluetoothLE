using System;
using System.Threading;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
#endif

using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Generators
{
    public abstract class Generator : PollingBlock
    {
        long startTicks;

        public OutputPort Output { get; private set; }

        public Generator (double updateFrequency = DefaultUpdateFrequency)
            : base (updateFrequency)
        {
            startTicks = DateTime.UtcNow.Ticks;

            Output = AddOutput ("Output", Units.Scalar, 0);
        }

        protected abstract double Generate (double time);

        protected override void Poll ()
        {
            var nowTicks = DateTime.UtcNow.Ticks;

            var t = (nowTicks - startTicks) / (10000000.0);

            Output.Value = Generate (t);
        }
    }
}
