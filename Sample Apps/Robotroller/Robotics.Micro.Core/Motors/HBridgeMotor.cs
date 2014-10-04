using System;
using Microsoft.SPOT;
using Robotics.Micro.Generators;

namespace Robotics.Micro.Motors
{
    public class HBridgeMotor : Block, IDCMotor
    {
        /// <summary>
        /// 0 - 1 for the speed.
        /// </summary>
        public InputPort SpeedInput { get; private set; }

        /// <summary>
        /// Not all motors are created equally. This number scales the Speed Input so
        /// that you can match motor speeds without changing your logic.
        /// </summary>
        public InputPort CalibrationInput { get; private set; }

        /// <summary>
        /// When true, the wheels spin "freely"
        /// </summary>
        public InputPort IsNeutralInput { get; private set; }

        /// <summary>
        /// Connect this to the First A pin on the H-Bridge
        /// </summary>
        public OutputPort A1Output { get; private set; }

        /// <summary>
        /// Connect this to the Second A pin on the H-Bridge
        /// </summary>
        public OutputPort A2Output { get; private set; }

        public OutputPort PwmDutyCycleOutput { get; private set; }
        public OutputPort PwmFrequencyOutput { get; private set; }

        

        const double DefaultFrequency = 1600;

        public HBridgeMotor (IPwm pwm = null)
        {
            CalibrationInput = AddInput ("CalibrationInput", Units.Ratio, 1);
            SpeedInput = AddInput ("SpeedInput", Units.Ratio, 0);
            IsNeutralInput = AddInput ("IsNeutralInput", Units.Boolean, 0);

            A1Output = AddOutput ("A1Output", Units.Digital, 0);
            A2Output = AddOutput ("A2Output", Units.Digital, 0);

            PwmFrequencyOutput = AddOutput ("PwmFrequencyOutput", Units.Frequency, DefaultFrequency);
            PwmDutyCycleOutput = AddOutput ("PwmDutyCycleOutput", Units.Ratio, SpeedInput.Value);

            // This is a direct connection.
            SpeedInput.ValueChanged += (s, e) => Update ();
            IsNeutralInput.ValueChanged += (s, e) => Update ();

            if (pwm != null) {
                PwmFrequencyOutput.ConnectTo (pwm.FrequencyInput);
                PwmDutyCycleOutput.ConnectTo (pwm.DutyCycleInput);                
            }
        }

        void Update ()
        {
            if (IsNeutralInput.Value > 0.5) {
                A1Output.Value = 0;
                A2Output.Value = 0;
                PwmDutyCycleOutput.Value = 0;
            }
            else {
                var calSpeed = SpeedInput.Value * CalibrationInput.Value;
                var speed = System.Math.Min (System.Math.Abs (calSpeed), 1);
                var rev = calSpeed < 0;
                
                A1Output.Value = rev ? 0 : 1;
                A2Output.Value = rev ? 1 : 0;

                PwmDutyCycleOutput.Value = speed;
            }
        }

        public static HBridgeMotor CreateForNetduino (Microsoft.SPOT.Hardware.Cpu.PWMChannel enable, Microsoft.SPOT.Hardware.Cpu.Pin a1, Microsoft.SPOT.Hardware.Cpu.Pin a2)
        {
            var leftPwm = new Robotics.Micro.Devices.PwmOutputPin (enable);
            var leftMotor = new HBridgeMotor (leftPwm);
            leftMotor.A1Output.ConnectTo (new Robotics.Micro.Devices.DigitalOutputPin (a1).Input);
            leftMotor.A2Output.ConnectTo (new Robotics.Micro.Devices.DigitalOutputPin (a2).Input);
            return leftMotor;
        }
    }
}
