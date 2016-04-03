using System;
using Robotics.Micro.Devices;
using Robotics.Micro.Sensors.Buttons;

namespace Robotics.Micro.Core.WindowsIoTTests
{
    public class TestButtonDirectToLed : Test
    {
        int buttonHardware = 23;
        int ledHardware = 24;
        DigitalInputPin button;
        DigitalOutputPin led;

        public override string Title {
            get {
                return "Button Direct to LED";
            }
        }

        public override void Start ()
        {
            // Create the blocks
            button = new DigitalInputPin (buttonHardware);
            led = new DigitalOutputPin (ledHardware);

            // Connect them
            button.Output.ConnectTo (led.Input);
        }

        public override void Stop()
        {
            button.Stop();
            led.Stop();
        }
    }

    public class TestPushButton : Test
    {
        int buttonHardware = 23;
        int ledHardware = 24;
        DigitalInputPin button;
        DigitalOutputPin led;

        public override string Title {
            get {
                return "PushButton to LED";
            }
        }

        public override void Start ()
        {
            // Create the blocks
            button = new DigitalInputPin (buttonHardware);
            var pushButton = new PushButton ();
            led = new DigitalOutputPin (ledHardware);

            // Connect them
            button.Output.ConnectTo (pushButton.DigitalInput);

            var ledState = 0;
            led.Input.Value = ledState;

            pushButton.Clicked += (s, e) => {
                ledState = 1 - ledState;
                led.Input.Value = ledState;
            };
        }

        public override void Stop()
        {
            button.Stop();
            led.Stop();
        }
    }
}
