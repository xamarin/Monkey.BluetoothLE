using System;
using Microsoft.SPOT;
using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Sensors.Proximity
{
    public class SharpGP2D12 : Block, IRangeFinder
    {
        public InputPort AnalogInput { get; private set; }
        public OutputPort DistanceOutput { get; private set; }

        LookupTable lookup;

        // Make sure these are in the range of the lookup table
        // so we can easily test if we're beyond the max distance

        public const double MaxDistance = 0.79;
        public const double MinDistance = 0.098;

        // From http://www.sharpsma.com/webfm_send/1203

        public SharpGP2D12 ()
        {
            AnalogInput = new InputPort (this, "AnalogInput", Units.Ratio);
            DistanceOutput = new OutputPort (this, "DistanceOutput", Units.Distance, 0);

            lookup = new LookupTable {
                { 0.0912, 0.7904 },
                { 0.1086, 0.6472 },
                { 0.1476, 0.4352 },
                { 0.2094, 0.2912 },
                { 0.2976, 0.196 },
                { 0.3876, 0.1456 },
                { 0.528, 0.0976 },
            };

            lookup.Input.ConnectTo (AnalogInput);
            lookup.Output.ConnectTo (DistanceOutput);
        }
    }
}
