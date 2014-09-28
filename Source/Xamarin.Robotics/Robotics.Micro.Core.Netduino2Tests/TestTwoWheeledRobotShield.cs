using System;
using Microsoft.SPOT;
using Robotics.Micro.Motors;
using Robotics.Micro.Core.Netduino2Tests;
using Robotics.Micro.Generators;

namespace Robotics.Micro.Core.Netduino2Tests
{
    public class TestDrunkenRobotWithMotorShield
    {
        public static void Run()
        {
            var shield = new AdafruitMotorShield();
            var robot = new TwoWheeledRobot(shield.GetMotor(1), shield.GetMotor(2));

            new SineWave(0.1, 0.5, 0.5, updateFrequency: 9).Output.ConnectTo(robot.SpeedInput);
            new SineWave(0.5, 0.333, 0, updateFrequency: 11).Output.ConnectTo(robot.DirectionInput);
        }
    }
}
