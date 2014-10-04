using System;
using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Sensors.Motion
{
    public class Memsic2125 : Block
    {
		public InputPort XPwmInput { get; private set; }
		public InputPort YPwmInput { get; private set; }

        public OutputPort XAccelerationOutput { get; private set; }
        public OutputPort YAccelerationOutput { get; private set; }

        public Memsic2125()
        {
			XPwmInput = AddInput ("XPwmInput", Units.Digital);
			YPwmInput = AddInput ("YPwmInput", Units.Digital);

            XAccelerationOutput = AddOutput ("XAcceleration", Units.EarthGravity);
            YAccelerationOutput = AddOutput ("YAcceleration", Units.EarthGravity);

            CreateProcessor (XPwmInput, XAccelerationOutput);
            CreateProcessor (YPwmInput, YAccelerationOutput);
        }

        static void CreateProcessor (Port pwmInput, Port accelOutput)
        {
            var meter = new DutyCycleMeter ();
            var converter = new Transform {
                Function = DutyCycleToG,
            };
            pwmInput.ConnectTo (meter.PwmInput);
            meter.DutyCycleOutput.ConnectTo (converter.Input);
            converter.Output.ConnectTo (accelOutput);
        }

        // MEMSIC MXD2125G/M/N/H Rev.E Page 5
        const float GPerDutyCycle = 1.0f / 0.125f;

        static double DutyCycleToG(double dutyCycle)
        {
            var g = (dutyCycle - 0.5f) * GPerDutyCycle;
            return g;
        }
    }
}
