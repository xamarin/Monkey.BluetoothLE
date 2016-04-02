using System;
using System.Threading;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
#endif

namespace Robotics.Micro.SpecializedBlocks
{
    public abstract class PollingBlock : Block
    {
        public const double DefaultUpdateFrequency = 1;

        public Port UpdateFrequency { get; private set; }

        readonly BlockThread th;
        Timer timer;

        public PollingBlock (double updateFrequency = DefaultUpdateFrequency)
        {
            UpdateFrequency = AddPort ("UpdateFrequency", Units.Frequency, updateFrequency);
            UpdateFrequency.ValueChanged += UpdateFrequency_ValueChanged;

            th = BlockThread.Start (delegate {
                timer = new Timer (Timer_Tick, null, 89, PeriodMillis);

                for (; ; ) {
                    BlockThread.Sleep (3129);
                }
            });
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
