using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.Threading;
using Robotics.Micro.Devices;
using Robotics.Micro.Generators;
using Robotics.Micro.Motors;
using Robotics.Micro.Sensors.Proximity;
using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Core.Netduino2Tests
{
    public class TestTwoEyedRobotWithHbridge
    {
        public static void Run ()
        {
            var scope = new DebugScope ();

            //
            // Create the robot
            //
            var leftMotor = HBridgeMotor.CreateForNetduino (PWMChannels.PWM_PIN_D3, Pins.GPIO_PIN_D1, Pins.GPIO_PIN_D2);
            leftMotor.CalibrationInput.Value = 1;

            var rightMotor = HBridgeMotor.CreateForNetduino (PWMChannels.PWM_PIN_D6, Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D5);
            rightMotor.CalibrationInput.Value = 1;

            //
            // Create his eyes
            //
            var leftRange = new SharpGP2D12 { Name = "LeftRange" };
            var rightRange = new SharpGP2D12 { Name = "RightRange" };
            leftRange.AnalogInput.ConnectTo (new AnalogInputPin (AnalogChannels.ANALOG_PIN_A0, 10).Analog);
            rightRange.AnalogInput.ConnectTo (new AnalogInputPin (AnalogChannels.ANALOG_PIN_A1, 10).Analog);
            
            scope.ConnectTo (leftRange.DistanceOutput);
            scope.ConnectTo (rightRange.DistanceOutput);

            //
            // Now some intelligence
            // Each motor is driven by the distance sensor's reading
            //
            var nearDist = 0.1;
            var farDist = 0.5;
            var minSpeed = 0.4;

            TransformFunction distanceToSpeed = d => {
                if (d < nearDist) {
                    return -minSpeed;
                }
                if (d > farDist) {
                    return 1;
                }
                var a = (d - nearDist) / (farDist - nearDist);
                return a * (1 - minSpeed) + minSpeed;
            };

            var leftSpeed = new Transform (distanceToSpeed);
            leftSpeed.Input.ConnectTo (leftRange.DistanceOutput);
            leftSpeed.Output.ConnectTo (leftMotor.SpeedInput);

            var rightSpeed = new Transform (distanceToSpeed);
            rightSpeed.Input.ConnectTo (rightRange.DistanceOutput);
            rightSpeed.Output.ConnectTo (rightMotor.SpeedInput);
        }
    }

    public class TestDrunkenRobotWithHbridge
    {
        public static void Run ()
        {
            var leftMotor = HBridgeMotor.CreateForNetduino (PWMChannels.PWM_PIN_D3, Pins.GPIO_PIN_D1, Pins.GPIO_PIN_D2);
            var rightMotor = HBridgeMotor.CreateForNetduino (PWMChannels.PWM_PIN_D6, Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D5);

            var robot = new TwoWheeledRobot (leftMotor, rightMotor);

            new SineWave (0.1, 0.5, 0.5, updateFrequency: 9).Output.ConnectTo (robot.SpeedInput);
            new SineWave (0.5, 0.333, 0, updateFrequency: 11).Output.ConnectTo (robot.DirectionInput);
        }
    }

    public class TestWallBouncingRobotWithHBridge
    {
        public static void Run ()
        {
            //
            // Start with the basic robot
            //
            var leftMotor = HBridgeMotor.CreateForNetduino (PWMChannels.PWM_PIN_D3, Pins.GPIO_PIN_D1, Pins.GPIO_PIN_D2);
            var rightMotor = HBridgeMotor.CreateForNetduino (PWMChannels.PWM_PIN_D6, Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D5);
            var robot = new TwoWheeledRobot (leftMotor, rightMotor);

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
                    new Connection (new Constant (0.6).Output, robot.SpinInput),
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
