using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using HWPort = Microsoft.SPOT.Hardware.Port;

namespace Robotics.Micro.Devices
{
    public class DigitalInputPin : Block
    {
        public Port Output { get; private set; }

        InterruptPort port;

        public DigitalInputPin (Cpu.Pin pin, bool glitchFilter = false)
		{
            port = new InterruptPort (pin, glitchFilter, HWPort.ResistorMode.Disabled, HWPort.InterruptMode.InterruptEdgeBoth);

            var initialValue = port.Read () ? 1 : 0;
            
            Output = AddPort ("Output", Units.Digital, initialValue);

            port.OnInterrupt += port_OnInterrupt;
		}

        void port_OnInterrupt (uint data1, uint data2, DateTime time)
        {
            Output.Value = data2;
        }
    }
}
