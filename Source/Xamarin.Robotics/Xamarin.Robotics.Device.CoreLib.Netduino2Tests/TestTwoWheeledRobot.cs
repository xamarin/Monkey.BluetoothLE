using System;
using Microsoft.SPOT;
using System.Threading;
using Xamarin.Robotics.Device.Motors;

namespace Xamarin.Robotics.Device.CoreLib.Netduino2Tests
{
    public class TestTwoWheeledRobot
    {
        public static void Run ()
        {
            var shield = new AdafruitMotorShield ();

            var leftMotor = shield.GetMotor (1);
            //var rightMotor = shield.GetMotor (2);

            leftMotor.DirectionInput.Value = 1;
            leftMotor.SpeedInput.Value = 0.1;

            for (; ; ) {
                Debug.Print ("Two wheels!!!");
                leftMotor.SpeedInput.Value += 0.05;
                Thread.Sleep (1000);
            }
        }
    }
}
