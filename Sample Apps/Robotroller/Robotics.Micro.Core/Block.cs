using System;
using System.Threading;

namespace Robotics.Micro
{
	public abstract class Block
	{
		string name = null;
		public string Name {
			get {
				if (name == null) {
					name = GetType ().Name;
				}
				return name;
			}
			set {
				name = value;
			}
		}

		static Block ()
		{
#if !MF_FRAMEWORK_VERSION_V4_3
			sw = new System.Diagnostics.Stopwatch ();
			sw.Start ();
#endif
		}

        protected InputPort AddInput (string name, Units units, double initialValue = 0)
		{
			return new InputPort (this, name, units, initialValue);
		}

        protected OutputPort AddOutput (string name, Units units, double initialValue = 0)
		{
			return new OutputPort (this, name, units, initialValue);
		}

        protected ConfigPort AddConfig (string name, Units units, double initialValue = 0)
        {
            return new ConfigPort (this, name, units, initialValue);
        }

        protected Port AddPort (string name, Units units, double initialValue = 0)
        {
            return new Port (this, name, units, initialValue);
        }

		public override string ToString ()
		{
			return GetType ().Name + " " + Name;
		}

#if !MF_FRAMEWORK_VERSION_V4_3
		static readonly System.Diagnostics.Stopwatch sw;
#endif

		protected double Time ()
		{
#if MF_FRAMEWORK_VERSION_V4_3
            return DateTime.UtcNow.Ticks * 100.0e-9;
#else
			return sw.Elapsed.Ticks * 100.0e-9;
#endif
		}

        protected void DebugWriteLine (string text = "")
        {
            var t = (long)(Time () * 1000.0 + 0.5);
            var msg = t + " " +
                Name + "[" + Thread.CurrentThread.ManagedThreadId + "]: " + 
                text;
#if MF_FRAMEWORK_VERSION_V4_3
            Microsoft.SPOT.Debug.Print (msg);
#else
            System.Diagnostics.Debug.WriteLine (msg);
#endif
        }

	}
}

