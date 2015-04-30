using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.IO.Ports;
using System.Threading;
using Robotics.Messaging;
using Robotics.Micro.Devices;
using Robotics.Micro.Generators;
using Robotics.Micro.Motors;
using Robotics.Micro.Sensors.Proximity;
using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Core.Netduino2Tests
{
    public class TestRCCar
    {
        public static void Run ()
        {
            //
            // Controls server
            //
            // initialize the serial port for COM1 (using D0 & D1)
            // initialize the serial port for COM3 (using D7 & D8)
            var serialPort = new SerialPort (SerialPorts.COM3, 57600, Parity.None, 8, StopBits.One);
            serialPort.Open ();
            var server = new ControlServer (serialPort);

            //
            // Just some diagnostic stuff
            //
            var uptimeVar = server.RegisterVariable ("Uptime (s)", 0);

            var lv = false;
            var led = new Microsoft.SPOT.Hardware.OutputPort (Pins.ONBOARD_LED, lv);

            //
            // Make the robot
            //
            var leftMotor = HBridgeMotor.CreateForNetduino (PWMChannels.PWM_PIN_D3, Pins.GPIO_PIN_D1, Pins.GPIO_PIN_D2);
            var rightMotor = HBridgeMotor.CreateForNetduino (PWMChannels.PWM_PIN_D6, Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D5);

            var robot = new TwoWheeledRobot (leftMotor, rightMotor);

            //
            // Expose some variables to control it
            //
            robot.SpeedInput.ConnectTo (server, writeable: true, name: "Speed");
            robot.DirectionInput.ConnectTo (server, writeable: true, name: "Turn");

            leftMotor.SpeedInput.ConnectTo (server);
            rightMotor.SpeedInput.ConnectTo (server);

            //
            // Show diagnostics
            //
            for (var i = 0; true; i++) {
                uptimeVar.Value = i;

                led.Write (lv);
                lv = !lv;
                Thread.Sleep (1000);
            }
        }
    }
}
