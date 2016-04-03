using System;
using Robotics.Micro.Devices;
using Robotics.Micro.Sensors.Buttons;

namespace Robotics.Micro.Core.WindowsIoTTests
{
    public class TestButtonDirectToLed : Test
    {
        int buttonHardware = 1;
        int ledHardware = 2;

        public override string Title {
            get {
                return "Button Direct to LED";
            }
        }

        public override void Run ()
        {
            // Create the blocks
            var button = new DigitalInputPin (buttonHardware);
            var led = new DigitalOutputPin (ledHardware);

            // Connect them
            button.Output.ConnectTo (led.Input);
        }
    }

    public class TestPushButton : Test
    {
        int buttonHardware = 1;
        int ledHardware = 2;

        public override string Title {
            get {
                return "PushButton to LED";
            }
        }

        public override void Run ()
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
        }
    }
}
