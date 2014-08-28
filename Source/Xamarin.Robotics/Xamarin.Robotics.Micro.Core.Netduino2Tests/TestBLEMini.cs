using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.IO.Ports;
using System.Threading;
using Xamarin.Robotics.Micro.Devices;
using Xamarin.Robotics.Micro.Generators;
using Xamarin.Robotics.Micro.Motors;
using Xamarin.Robotics.Micro.Sensors.Proximity;
using Xamarin.Robotics.Micro.SpecializedBlocks;

namespace Xamarin.Robotics.Micro.Core.Netduino2Tests
{
    public class TestBLEMini
    {
        static SerialPort serial;

        public static void Run ()
        {
            // initialize the serial port for COM1 (using D0 & D1)
            // initialize the serial port for COM3 (using D7 & D8)
            serial = new SerialPort (SerialPorts.COM3, 57600, Parity.None, 8, StopBits.One);
            // open the serial-port, so we can send & receive data
            serial.Open ();
            // add an event-handler for handling incoming data
            serial.DataReceived += new SerialDataReceivedEventHandler (serial_DataReceived);

            var led = new Microsoft.SPOT.Hardware.OutputPort (Pins.ONBOARD_LED, false);
            var lv = false;
            // write forever...
            for (var i = 0; true; i++) {
                serial.Write (new byte[] { 0xAA, 0x55, (byte)i }, 0, 3);

                led.Write (lv);
                lv = !lv;
                Thread.Sleep (2000);
            }            
        }

        static void serial_DataReceived (object sender, SerialDataReceivedEventArgs e)
        {
            // create a single byte array
            byte[] bytes = new byte[1];

            // as long as there is data waiting to be read
            while (serial.BytesToRead > 0) {
                // read a single byte
                serial.Read (bytes, 0, bytes.Length);
                // send the same byte back
                serial.Write (bytes, 0, bytes.Length);
            }
        }
    }
}
