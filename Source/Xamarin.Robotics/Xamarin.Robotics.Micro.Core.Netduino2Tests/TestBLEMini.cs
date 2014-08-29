using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.IO.Ports;
using System.Threading;
using Xamarin.Robotics.Messaging;
using Xamarin.Robotics.Micro.Devices;
using Xamarin.Robotics.Micro.Generators;
using Xamarin.Robotics.Micro.Motors;
using Xamarin.Robotics.Micro.Sensors.Proximity;
using Xamarin.Robotics.Micro.SpecializedBlocks;

namespace Xamarin.Robotics.Micro.Core.Netduino2Tests
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

            var iVar = server.RegisterVariable ("eye", 0);

            for (var i = 0; true; i++) {

                server.SetVariableValue (iVar, i);

                led.Write (lv);
                lv = !lv;
                Thread.Sleep (1000);
            }            
        }
    }
}
