using System;
using System.Threading;

namespace Xamarin.Robotics
{
	public class Pwm : BlockBase
	{
		public Port Output { get; private set; }

		public Port DutyCycleInput { get; private set; }
		public Port FrequencyInput { get; private set; }

		Thread th;

		public Pwm ()
		{
			Output = AddPort ("Output", Units.Digital);
			DutyCycleInput = AddPort ("DutyCycleInput", Units.Ratio, 0.5);
			FrequencyInput = AddPort ("FrequencyInput", Units.Frequency, 1);

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

