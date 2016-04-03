using System;
using Windows.Devices.Pwm;

using Robotics.Micro.Generators;

namespace Robotics.Micro.Devices
{
    public class PwmOutputPin : Block, IPwm
    {
        public InputPort DutyCycleInput { get; private set; }
        public InputPort FrequencyInput { get; private set; }

        PwmPin pwm;

        public PwmOutputPin (int pinNumber, double frequencyHz = 1000, double dutyCycle = 0)
        {
            DutyCycleInput = AddInput("DutyCycleInput", Units.Ratio, dutyCycle);
            FrequencyInput = AddInput("FrequencyInput", Units.Frequency, frequencyHz);

            var pwmc = PwmController.GetDefaultAsync().GetResults();
            if (pwmc == null) {
                Error("No Default PWM Controller");
            }
            else {
                pwmc.SetDesiredFrequency(frequencyHz);

                pwm = pwmc.OpenPin(pinNumber);
                pwm.SetActiveDutyCyclePercentage(DutyCycleInput.Value * 100.0);

                DutyCycleInput.ValueChanged += (s, e) =>
                {
                    pwm.SetActiveDutyCyclePercentage(DutyCycleInput.Value * 100.0);
                };

                FrequencyInput.ValueChanged += (s, e) =>
                {
                    pwmc.SetDesiredFrequency(frequencyHz);
                };

                pwm.Start();
            }
        }
    }
}
