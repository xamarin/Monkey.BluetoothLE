using System;
using System.Threading;

namespace Robotics.Micro.Generators
{
    /// <summary>
    /// A Pulse Width Modulation Generator that can
    /// generates waveforms in software. The maximum
    /// Frequency is about 100 Hz.
    /// </summary>
	public class SoftPwm : Block
	{
		public OutputPort Output { get; private set; }

		public InputPort DutyCycleInput { get; private set; }
		public InputPort FrequencyInput { get; private set; }

		Thread th;

		public SoftPwm ()
		{
			Output = AddOutput ("Output", Units.Digital);
			DutyCycleInput = AddInput ("DutyCycleInput", Units.Ratio, 0.5);
            FrequencyInput = AddInput ("FrequencyInput", Units.Frequency, 1);

			th = new Thread ((ThreadStart)delegate {

				for (;;) {
					Output.Value = 1;
					Thread.Sleep (OnTimeMillis);
					Output.Value = 0;
					Thread.Sleep (OffTimeMillis);
				}

			});
			th.Start ();
		}

		int OnTimeMillis
		{
			get
			{
				return (int)(DutyCycleInput.Value / FrequencyInput.Value * 1000);
			}
		}

		int OffTimeMillis
		{
			get
			{
				return (int)(((1 - DutyCycleInput.Value) / FrequencyInput.Value)*1000);
			}
		}
	}
}

