using System;
using Microsoft.SPOT;
using System.Threading;
using Xamarin.Robotics.Motors;
using Xamarin.Robotics.SpecializedBlocks;

namespace Xamarin.Robotics.Device.CoreLib.Netduino2Tests
{
    public class TwoWheeledRobot : BlockBase
    {
        IDCMotor leftMotor;
        IDCMotor rightMotor;

        /// <summary>
        /// Speed from 0 to 1.
        /// </summary>
        public InputPort SpeedInput { get; private set; }

        /// <summary>
        /// Direction to travel. 0 = straight ahead. -1 if full left turn, +1 is full right turn.
        /// </summary>
        public InputPort DirectionInput { get; private set; }

        public TwoWheeledRobot (IDCMotor leftMotor, IDCMotor rightMotor)
        {
            this.leftMotor = leftMotor;
            this.rightMotor = rightMotor;

            DirectionInput = new InputPort (this, "DirectionInput", Units.Scalar);
            SpeedInput = new InputPort (this, "SpeedInput", Units.Ratio);

            Update ();

            SpeedInput.ValueChanged += (s, e) => Update ();
            DirectionInput.ValueChanged += (s, e) => Update ();
        }

        void Update ()
        {
            Debug.Print ("Robot Direction = " + DirectionInput.Value);

            // The motors always move forwards
            leftMotor.DirectionInput.Value = 1;
            rightMotor.DirectionInput.Value = 1;

            var dir = System.Math.Max (System.Math.Min (DirectionInput.Value, 1), -1);
            var spd = System.Math.Max (System.Math.Min (SpeedInput.Value, 1), 0);

            if (System.Math.Abs (dir) < 0.01) {
                leftMotor.SpeedInput.Value = spd;
                rightMotor.SpeedInput.Value = spd;
                return;
            }

            // We turn by slowing one wheel down in proportion
            // to the steepness of the turn
            if (dir < 0) {
                // To turn left, favor the right wheel
                leftMotor.SpeedInput.Value = spd * (1 + dir);
                rightMotor.SpeedInput.Value = spd;
            }
            else {
                // To turn right, favor the left wheel
                leftMotor.SpeedInput.Value = spd;
                rightMotor.SpeedInput.Value = spd * (1 - dir);
            }
        }
    }

    public class TestTwoWheeledRobot
    {
        public static void Run ()
        {
            var shield = new AdafruitMotorShield ();

            var leftMotor = shield.GetMotor (1);
            var rightMotor = shield.GetMotor (2);

            var robot = new TwoWheeledRobot (leftMotor, rightMotor);

            new SineWaveGenerator (0.1, 0.5, 0.5, updateFrequency: 4).Output.ConnectTo (robot.SpeedInput);

            new SineWaveGenerator (0.5, 0.5, 0, updateFrequency: 10).Output.ConnectTo (robot.DirectionInput);

            for (; ; ) {
                Debug.Print ("TwoWheeled Tick");

                Thread.Sleep (1000);
            }
        }
    }
}
