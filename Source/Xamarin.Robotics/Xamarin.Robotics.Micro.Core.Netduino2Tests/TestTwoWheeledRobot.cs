using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.Threading;
using Xamarin.Robotics.Generators;
using Xamarin.Robotics.Motors;
using Xamarin.Robotics.Sensors.Proximity;
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

        /// <summary>
        /// Direction to spin. 0 = no spinning. -1 if full left spinning, +1 is full right spinning.
        /// Spinning is an exclusive action of driving with Speed and Direction
        /// </summary>
        public InputPort SpinInput { get; private set; }

        public TwoWheeledRobot (IDCMotor leftMotor, IDCMotor rightMotor)
        {
            this.leftMotor = leftMotor;
            this.rightMotor = rightMotor;

            DirectionInput = AddInput ("DirectionInput", Units.Scalar);
            SpeedInput = AddInput ("SpeedInput", Units.Ratio);
            SpinInput = AddInput ("SpinInput", Units.Scalar);

            Update ();

            SpeedInput.ValueChanged += (s, e) => Update ();
            DirectionInput.ValueChanged += (s, e) => Update ();
            SpinInput.ValueChanged += (s, e) => Update ();
        }

        void Update ()
        {
            var spinning = System.Math.Abs (SpinInput.Value) > 0.01;
            if (spinning) {

                var rate = System.Math.Min (System.Math.Abs (SpinInput.Value), 1);

                if (SpinInput.Value < 0) {
                    leftMotor.SpeedInput.Value = -rate;
                    rightMotor.SpeedInput.Value = rate;
                }
                else {
                    leftMotor.SpeedInput.Value = rate;
                    rightMotor.SpeedInput.Value = -rate;
                }

            }
            else {
                // Debug.Print ("Robot Direction = " + DirectionInput.Value);

                // The motors always move forwards
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
    }

    public class TestDrunkenRobot
    {
        public static void Run ()
        {
            var shield = new AdafruitMotorShield ();
            var robot = new TwoWheeledRobot (shield.GetMotor (1), shield.GetMotor (2));

            new SineWave (0.1, 0.5, 0.5, updateFrequency: 9).Output.ConnectTo (robot.SpeedInput);
            new SineWave (0.5, 0.333, 0, updateFrequency: 11).Output.ConnectTo (robot.DirectionInput);
        }
    }

    public class TestWallBouncingRobot
    {
        public static void Run ()
        {
            //
            // Start with the basic robot
            //
            var shield = new AdafruitMotorShield ();
            var robot = new TwoWheeledRobot (shield.GetMotor (1), shield.GetMotor (2));

            //
            // Create a range finder and scope it
            //
            var scope = new DebugScope ();
            var a0 = new AnalogInputPin (AnalogChannels.ANALOG_PIN_A0, 10);
            scope.ConnectTo (a0.Analog);
            var sharp = new SharpGP2D12 ();
            a0.Analog.ConnectTo (sharp.AnalogInput);
            scope.ConnectTo (sharp.DistanceOutput);
            scope.ConnectTo (robot.SpeedInput);

            //
            // This is the cruising (unobstructed) speed
            //
            var distanceToSpeed = new Transform (distance => {
                const double min = 0.1;
                const double max = 0.5;
                if (distance > max) {
                    return 1.0;
                }
                else if (distance < min) {
                    return 0.0;
                }
                return (distance - min) / (max - min);
            });
            distanceToSpeed.Input.ConnectTo (sharp.DistanceOutput);
            
            //
            // Take different actions depending on our environment:
            //   0: cruising
            //   1: collided
            //
            var sw = new Switch (
                new [] {
                    new Connection (distanceToSpeed.Output, robot.SpeedInput),
                    new Connection (new SineWave (0.5, 0.333, 0, updateFrequency: 10).Output, robot.DirectionInput),
                    new Connection (new Constant (0).Output, robot.SpinInput),
                },
                new[] {
                    new Connection (new Constant (0.3).Output, robot.SpinInput),
                    new Connection (new Constant (0).Output, robot.DirectionInput),
                    new Connection (new Constant (0).Output, robot.SpeedInput),
                });

            var collided = new Transform (distance => distance < 0.2 ? 1 : 0);
            collided.Input.ConnectTo (sharp.DistanceOutput);
            collided.Output.ConnectTo (sw.Input);

            //
            // Loop to keep us alive
            //
            for (; ; ) {
                //Debug.Print ("TwoWheeled Tick");

                Thread.Sleep (1000);
            }
        }
    }
}
