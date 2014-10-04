using System;
using Microsoft.SPOT;
using Robotics.Micro.Devices;
using SecretLabs.NETMF.Hardware.Netduino;
using Microsoft.SPOT.Hardware;
using Robotics.Micro.Sensors.Buttons;

namespace Robotics.Device.CoreLib.Netduino2Tests
{
    public class TestButtonDirectToLed
    {
        Cpu.Pin buttonHardware = Pins.ONBOARD_BTN;
        Cpu.Pin ledHardware = Pins.ONBOARD_LED;

        public void Run ()
        {
            // Create the blocks
            var button = new DigitalInputPin (buttonHardware);
            var led = new DigitalOutputPin (ledHardware);

            // Connect them
            button.Output.ConnectTo (led.Input);

            // Do nothing
            for (; ; ) {
                System.Threading.Thread.Sleep (1000);
            }
        }
    }

    public class TestPushButton
    {
        Cpu.Pin buttonHardware = Pins.ONBOARD_BTN;
        Cpu.Pin ledHardware = Pins.ONBOARD_LED;

        public void Run ()
        {
            // Create the blocks
            var button = new DigitalInputPin (buttonHardware);
            var pushButton = new PushButton ();
            var led = new DigitalOutputPin (ledHardware);

            // Connect them
            button.Output.ConnectTo (pushButton.DigitalInput);

            var ledState = 0;
            led.Input.Value = ledState;

            pushButton.Clicked += (s, e) => {
                ledState = 1 - ledState;
                led.Input.Value = ledState;
            };

            // Do nothing
            for (; ; ) {
                System.Threading.Thread.Sleep (1000);
            }
        }
    }
}
