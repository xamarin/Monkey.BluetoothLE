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

            PwmController pwmc = null;
            try
            {
                pwmc = PwmController.GetDefaultAsync().AsTask().Result;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
            if (pwmc == null) {
                Error("No Default PWM Controller");
            }
            else {
                pwmc.SetDesiredFrequency(frequencyHz);

                pwm = pwmc.OpenPin(pinNumber);
                pwm.SetActiveDutyCyclePercentage(DutyCycleInput.Value * 100.0);

                DutyCycleInput.ValueChanged += (s, e) =>
                {
                    if (pwm != null)
                    {
                        pwm.SetActiveDutyCyclePercentage(DutyCycleInput.Value * 100.0);
                    }
                };

                FrequencyInput.ValueChanged += (s, e) =>
                {
                    pwmc.SetDesiredFrequency(frequencyHz);
                };

                pwm.Start();
            }
        }

        public void Stop ()
        {
            var p = pwm;
            pwm = null;

            if (p != null)
            {                
                p.Dispose();
            }
        }
    }
}
