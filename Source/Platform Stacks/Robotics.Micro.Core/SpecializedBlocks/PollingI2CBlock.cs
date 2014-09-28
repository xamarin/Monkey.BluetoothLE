using System;
using System.Threading;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
#endif

namespace Robotics.Micro.SpecializedBlocks
{
    public abstract class PollingI2CBlock : I2CBlock
    {
        public Port UpdateFrequency { get; private set; }

        readonly Thread th;
        Timer timer;

        public PollingI2CBlock (byte address, int clockRateKhz = DefaultClockRate)
            : base (address, clockRateKhz)
        {
            UpdateFrequency = AddPort ("UpdateFrequency", Units.Frequency, 1);
            UpdateFrequency.ValueChanged += UpdateFrequency_ValueChanged;

            th = new Thread ((ThreadStart)delegate {
                timer = new Timer (Timer_Tick, null, 89, PeriodMillis);

                for (; ; ) {
                    Thread.Sleep (3129);
                }
            });
            th.Start ();
        }

        int PeriodMillis
        {
            get
            {
                return (int)(1000 / UpdateFrequency.Value);
            }
        }

        void UpdateFrequency_ValueChanged (object sender, EventArgs e)
        {
            timer.Change (PeriodMillis, PeriodMillis);
        }

        void Timer_Tick (object state)
        {
            Poll ();
        }

        protected abstract void Poll ();
    }
}
