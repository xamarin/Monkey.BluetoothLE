using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using Xamarin.Robotics.Micro.Generators;

using HWPort = Microsoft.SPOT.Hardware.Port;
using HWOutputPort = Microsoft.SPOT.Hardware.OutputPort;

namespace Xamarin.Robotics.Micro.Devices
{
    public class PwmOutputPin : BlockBase, IPwm
    {
        public InputPort DutyCycleInput { get; private set; }
        public InputPort FrequencyInput { get; private set; }

        PWM pwm;

        public PwmOutputPin (Microsoft.SPOT.Hardware.Cpu.PWMChannel channel, double frequencyHz = 1000, double dutyCycle = 0)
        {
            pwm = new PWM (channel, frequencyHz, dutyCycle, false);

            DutyCycleInput = AddInput ("DutyCycleInput", Units.Ratio, dutyCycle);
            FrequencyInput = AddInput ("FrequencyInput", Units.Frequency, frequencyHz);

            DutyCycleInput.ValueChanged += (s, e) => {
                pwm.DutyCycle = DutyCycleInput.Value;
            };

            FrequencyInput.ValueChanged += (s, e) => {
                pwm.Frequency = FrequencyInput.Value;
            };

            pwm.Start ();
        }
    }
}
