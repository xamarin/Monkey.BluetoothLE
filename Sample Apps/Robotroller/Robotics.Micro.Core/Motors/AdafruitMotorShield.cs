using System;
using Microsoft.SPOT;
using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Motors
{
    public class AdafruitMotorShield : I2CBlock
    {
        public const byte DefaultAddress = 0x60;

        const byte PCA9685_MODE1 = 0x0;
        const byte PCA9685_PRESCALE = 0xFE;

        public AdafruitMotorShield (uint pwmFrequency = DefaultPwmFrequency, byte address = DefaultAddress, int clockRateKhz = DefaultClockRate)
            : base (address, clockRateKhz)
        {
            Reset ();
            SetPwmFrequency (pwmFrequency);
            for (byte i = 0; i < NumPins; i++) {
                SetPwm (i, 0, 0);
            }
        }

        public void Reset ()
        {
            // Debug.Print ("AdafruitMotorShield.Reset ()");
            WriteByte (PCA9685_MODE1, 0);
        }

        const ushort MaxPwmValue = 4095; // Inclusive
        const ushort ConstantPwmValue = 4096;

        const byte NumPins = 16;

        public void SetPin (byte pin, bool value)
        {
            if (value) {
                SetPwm (pin, ConstantPwmValue, 0);
            }
            else {
                SetPwm (pin, 0, 0);
            }
        }

        public void SetPwm (byte pin, ushort value)
        {
            if (value > MaxPwmValue) {
                SetPwm (pin, ConstantPwmValue, 0);
            }
            else {
                SetPwm (pin, 0, value);
            }
        }

        AdafruitDCMotor[] motors = new AdafruitDCMotor[4];

        public AdafruitDCMotor GetMotor (int num)
        {
            var index = num - 1;
            if (index < 0 || index >= motors.Length)
                throw new ArgumentException ("Only motors 1-" + motors.Length + " are available.");

            var m = motors[index];
            if (m == null) {
                byte pwm, in1, in2;
                switch (index) {
                    case 0:
                        pwm = 8;
                        in2 = 9;
                        in1 = 10;
                        break;
                    case 1:
                        pwm = 13;
                        in2 = 12;
                        in1 = 11;
                        break;
                    case 2:
                        pwm = 2;
                        in2 = 3;
                        in1 = 4;
                        break;
                    default:
                        pwm = 7;
                        in2 = 6;
                        in1 = 5;
                        break;
                }
                m = new AdafruitDCMotor (this, pwm, in1, in2);
                motors[index] = m;
            }
            return m;
        }

        const uint DefaultPwmFrequency = 1600;

        void SetPwmFrequency (uint freqHz)
        {
            // Debug.Print ("Set PWM Frequency to " + freqHz + " hz");

            float prescaleval = 25000000;
            prescaleval /= 4096;
            prescaleval /= freqHz;
            prescaleval -= 1;
            byte prescale = (byte)System.Math.Floor (prescaleval + 0.5);

            var oldmode = ReadByte (PCA9685_MODE1);
            var newmode = (byte)((oldmode & 0x7F) | 0x10); // sleep
            WriteByte (PCA9685_MODE1, newmode); // go to sleep
            WriteByte (PCA9685_PRESCALE, prescale); // set the prescaler
            WriteByte (PCA9685_MODE1, oldmode);
            System.Threading.Thread.Sleep (5); // OMG
            WriteByte (PCA9685_MODE1, (byte)(oldmode | 0xA1));
        }

        const byte LED0_ON_L = 0x6;

        byte[] setPwmBuffer = new byte[4];

        void SetPwm (byte pin, ushort on, ushort off)
        {
            // Debug.Print ("Set PWM #" + pin + ": " + on + " / " + off);

            setPwmBuffer[0] = (byte)on;
            setPwmBuffer[1] = (byte)(on >> 8);
            setPwmBuffer[2] = (byte)off;
            setPwmBuffer[3] = (byte)(off >> 8);

            WriteBytes ((byte)(LED0_ON_L + 4 * pin), setPwmBuffer);
        }
    }

    public class AdafruitDCMotor : Block, IDCMotor
    {
        readonly AdafruitMotorShield shield;

        readonly byte pwmPin;
        readonly byte in1Pin;
        readonly byte in2Pin;

        /// <summary>
        /// Remember if we're reversed to minimize state changes
        /// </summary>
        bool isReversed = false; 

        /// <summary>
        /// The speed of the motor from -1 to 1.
        /// </summary>
        public InputPort SpeedInput { get; private set; }

        /// <summary>
        /// When true, the wheels spin "freely"
        /// </summary>
        public InputPort IsNeutralInput { get; private set; }

        public AdafruitDCMotor (AdafruitMotorShield shield, byte pwmPin, byte in1Pin, byte in2Pin)
        {
            this.shield = shield;
            this.pwmPin = pwmPin;
            this.in1Pin = in1Pin;
            this.in2Pin = in2Pin;

            SpeedInput = AddInput ("SpeedInput", Units.Ratio);
            IsNeutralInput = AddInput ("IsNeutralInput", Units.Boolean);
            
            SetDirection ();
            SetSpeed ();

            SpeedInput.ValueChanged += (s, e) => {

                var newlyReversed = SpeedInput.Value < 0;
                if (newlyReversed != isReversed) {
                    SetDirection ();
                }

                SetSpeed ();
            };
        }

        void SetSpeed ()
        {
            var speed = System.Math.Min (System.Math.Abs (SpeedInput.Value), 1);
            shield.SetPwm (pwmPin, (ushort)(4095 * speed));
        }

        void SetDirection ()
        {
            if (IsNeutralInput.Value >= 0.5) {
                shield.SetPin (in1Pin, false);
                shield.SetPin (in2Pin, false);
            }
            else {
                var d = SpeedInput.Value;

                // Note, always switch low (false) first to avoid glitches

                if (d < 0) {
                    // Reverse
                    shield.SetPin (in1Pin, false);
                    shield.SetPin (in2Pin, true);
                    isReversed = true;
                }
                else {
                    // Forward
                    shield.SetPin (in2Pin, false);
                    shield.SetPin (in1Pin, true);
                    isReversed = false;
                }
            }
        }
    }
}
