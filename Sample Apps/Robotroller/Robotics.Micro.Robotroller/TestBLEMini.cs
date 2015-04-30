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
    public class TestBLEMini
    {
        public static void Run ()
        {
            // initialize the serial port for COM1 (using D0 & D1)
            // initialize the serial port for COM3 (using D7 & D8)
            var serialPort = new SerialPort (SerialPorts.COM3, 57600, Parity.None, 8, StopBits.One);
            serialPort.Open ();

            var server = new ControlServer (serialPort);

            var led = new Microsoft.SPOT.Hardware.OutputPort (Pins.ONBOARD_LED, false);
            var lv = false;

            var a0 = new AnalogInput (AnalogChannels.ANALOG_PIN_A0, -1);
            var a1 = new AnalogInput (AnalogChannels.ANALOG_PIN_A1, -1);

            var uptimeVar = server.RegisterVariable ("Uptime (s)", 0);

            server.RegisterVariable ("Speed", 0, v => { });
            server.RegisterVariable ("Turn", 0, v => { });

            var a0Var = server.RegisterVariable ("Analog 0", 0);
            var a1Var = server.RegisterVariable ("Analog 1", 0);

            var magicCmd = server.RegisterCommand ("Magic", () => {
                Debug.Print ("MAAAGIIICC");
                return 42;
            });

            for (var i = 0; true; i++) {

                uptimeVar.Value = i;
                a0Var.Value = a0.Read ();
                a1Var.Value = a1.Read ();

                led.Write (lv);
                lv = !lv;
                Thread.Sleep (1000);
            }            
        }
    }
}
