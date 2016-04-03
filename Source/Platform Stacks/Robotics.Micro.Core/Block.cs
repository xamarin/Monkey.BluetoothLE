using System;

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

        protected void Error (string message)
        {
            DebugWriteLine("ERROR! " + message);
        }

        protected void DebugWriteLine (string text = "")
        {
            var t = (long)(Time () * 1000.0 + 0.5);
            var msg = t + " " +
                Name + "[" + BlockThread.CurrentId + "]: " + 
                text;
#if MF_FRAMEWORK_VERSION_V4_3
            Microsoft.SPOT.Debug.Print (msg);
#else
            System.Diagnostics.Debug.WriteLine (msg);
#endif
        }

	}

    public delegate void BlockThreadProc();

    public class BlockThread
    {
#if MF_FRAMEWORK_VERSION_V4_3
        System.Threading.Thread thread;
        public static BlockThread Start()
        {
            var t = new System.Threading.Thread ((ThreadStart)delegate {
                proc ();
            });
            t.Start ();
            return new BlockThread { thread = t };
        }
        public static int CurrentId
        {
            get
            {
                return Thread.CurrentThread.ManagedThreadId;
            }
        }
        public static void Sleep (int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }
        public void Stop ()
        {
            // Cannot stop on Netduino
        }
#else
#pragma warning disable 414 // Store a ref
        System.Threading.Tasks.Task task;
        System.Threading.CancellationTokenSource cts;
#pragma warning restore 414

        public void Stop ()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
        }

        public static BlockThread Start(BlockThreadProc proc)
        {
            var cts = new System.Threading.CancellationTokenSource();
            var t = System.Threading.Tasks.Task.Factory.StartNew(
                () => proc(),
                cts.Token,
                System.Threading.Tasks.TaskCreationOptions.LongRunning,
                System.Threading.Tasks.TaskScheduler.Default);
            return new BlockThread { task = t, cts = cts };
        }

        public static int CurrentId
        {
            get
            {
                var ido = System.Threading.Tasks.Task.CurrentId;
                return ido.HasValue ? (int)ido : -1;
            }
        }

        public static void Sleep (int milliseconds)
        {
            System.Threading.Tasks.Task.Delay(milliseconds).Wait();
        }
#endif
    }
}

