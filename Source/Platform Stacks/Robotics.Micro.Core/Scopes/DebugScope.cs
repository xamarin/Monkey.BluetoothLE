using System;
using System.Collections;
using System.Threading;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
#endif

namespace Robotics.Micro
{
    public class DebugScope : Block
    {
        public Port UpdatePeriod { get; private set; }

        Timer timer;

        public DebugScope ()
        {
            UpdatePeriod = AddPort ("UpdatePeriod", Units.Time, 5);

            timer = new Timer (Timer_Tick, null, 234, PeriodMillis);

            UpdatePeriod.ValueChanged += UpdatePeriod_ValueChanged;
        }

        int PeriodMillis
        {
            get
            {
                return (int)(UpdatePeriod.Value * 1000);
            }
        }

        void UpdatePeriod_ValueChanged (object sender, EventArgs e)
        {
            timer.Change (PeriodMillis, PeriodMillis);
        }

        readonly ArrayList connectedPorts = new ArrayList ();

        public void ConnectTo (Port port)
        {
            connectedPorts.Add (port);
        }

        void Timer_Tick (object state)
        {
            if (connectedPorts.Count < 1)
                return;

            DebugWriteLine ();
            foreach (Port p in connectedPorts) {
                var u = p.ValueUnits.ToShortString ();
                DebugWriteLine (p.FullName + " = " + p.Value + (u.Length > 0 ? " " + u : ""));
            }
            DebugWriteLine ();
        }
    }
}
