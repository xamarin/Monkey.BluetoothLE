using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.Threading;

namespace Robotics.Micro.Core.Netduino2Tests
{
	public class Program
	{
		public static void Main()
		{
            using (var led = new Microsoft.SPOT.Hardware.OutputPort (Pins.ONBOARD_LED, false)) {

                for (var i = 0; i < 3; i++) {
                    led.Write (true);
                    Thread.Sleep (250);
                    led.Write (false);
                    Thread.Sleep (250);
                }
            }

            // Frank tests
            // TestBLEMini.Run ();
            TestRCCar.Run ();

            // MotorShield (deprecated)
            // TestDrunkenRobotWithMotorShield.Run();

            // TestSensors.Run ();
            // TestDrunkenRobot.Run ();
            // TestWallBouncingRobotWithHBridge.Run ();
            
            // No eyes, just motors
            // TestDrunkenRobotWithHbridge.Run ();
            
            // Requires IR sensors
            // TestTwoEyedRobotWithHbridge.Run ();
		}
	}
}
