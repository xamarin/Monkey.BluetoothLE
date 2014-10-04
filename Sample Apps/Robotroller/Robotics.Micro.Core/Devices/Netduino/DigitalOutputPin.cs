using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using HWPort = Microsoft.SPOT.Hardware.Port;
using HWOutputPort = Microsoft.SPOT.Hardware.OutputPort;


namespace Robotics.Micro.Devices
{
    public class DigitalOutputPin : Block
    {
        public InputPort Input { get; private set; }

        HWOutputPort port;

        const double HighMinValue = 0.2;

        public DigitalOutputPin (Cpu.Pin pin, double initialValue = 0)
		{
            Input = AddInput ("Input", Units.Digital, initialValue);

            port = new HWOutputPort (pin, initialValue >= HighMinValue);

            Input.ValueChanged += (s, e) => {
                port.Write (Input.Value >= HighMinValue);
            };
		}
    }
}
