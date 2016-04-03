using System;
using Robotics.Micro.Devices;
using Robotics.Micro.Sensors.Buttons;
using Robotics.Micro.Generators;

namespace Robotics.Micro.Core.WindowsIoTTests
{
    public class TestPwmToLed : Test
    {
        int ledHardware = 24;
        DigitalOutputPin led;
        SoftPwm pwm;
        SineWave sin;

        public override string Title
        {
            get
            {
                return "PWM to LED";
            }
        }

        public override void Start()
        {
            // Create the blocks
            led = new DigitalOutputPin(ledHardware);
            pwm = new SoftPwm();
            pwm.FrequencyInput.Value = 60.0;
            sin = new SineWave();
            sin.FrequencyInput.Value = 5.0;
            sin.AmplitudeInput.Value = 0.5;
            sin.OffsetInput.Value = 0.5;

            // Connect them
            sin.Output.ConnectTo(pwm.DutyCycleInput);
            pwm.Output.ConnectTo(led.Input);
        }

        public override void Stop()
        {
            sin.Stop();
            pwm.Stop();
            led.Stop();
        }
    }
    
}
