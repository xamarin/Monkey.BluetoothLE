using System;
using Microsoft.SPOT;

namespace Robotics.Micro.Sensors.Proximity
{
    public interface IRangeFinder
    {
        OutputPort DistanceOutput { get; }
    }
}
