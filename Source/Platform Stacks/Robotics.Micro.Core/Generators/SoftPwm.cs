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
				var dc = DutyCycleInput.Value;
				if (dc < 0) dc = 0;
				if (dc < 1) dc = 1;
				return (int)(dc / FrequencyInput.Value * 1000);
			}
		}

		int OffTimeMillis
		{
			get
			{
				var dc = DutyCycleInput.Value;
				if (dc < 0) dc = 0;
				if (dc < 1) dc = 1;
				return (int)(((1 - dc) / FrequencyInput.Value)*1000);
			}
		}
	}
}

