using System;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
#endif


namespace Robotics.Micro.SpecializedBlocks
{
    public class DutyCycleMeter : Block
    {
		public InputPort PwmInput { get; private set; }
        
		public OutputPort DutyCycleOutput { get; private set; }
		public OutputPort FrequencyOutput { get; private set; }

		double upTime;
		double downTime;

        public DutyCycleMeter ()
        {
			upTime = Time ();
            downTime = upTime;

			PwmInput = AddInput ("PwmInput", Units.Digital);
			DutyCycleOutput = AddOutput ("DutyCycleOutput", Units.Ratio);
			FrequencyOutput = AddOutput ("FrequencyOutput", Units.Frequency);

            PwmInput.ValueChanged += PwmInput_ValueChanged;
        }

        void PwmInput_ValueChanged (object sender, EventArgs e)
        {
            var v = PwmInput.Value;

			var time = Time ();

            if (v > 0) {
                var totalTicks = (time - upTime);
                if (totalTicks > 0) {
					FrequencyOutput.Value = (1000.0 * 10000.0) / totalTicks;

                    var upTicks = (downTime - upTime);

                    if (upTicks >= 0) {
                        var DutyCycle = (double)upTicks / (double)totalTicks;
                        DutyCycleOutput.Value = DutyCycle;
                    }
                }

                upTime = time;
            }
            else {
                downTime = time;
            }

        }


    }
}
