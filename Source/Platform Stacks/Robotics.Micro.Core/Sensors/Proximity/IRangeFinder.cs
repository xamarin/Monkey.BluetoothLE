using System;

namespace Robotics.Micro.Sensors.Proximity
{
    public interface IRangeFinder
    {
        OutputPort DistanceOutput { get; }
    }
}
