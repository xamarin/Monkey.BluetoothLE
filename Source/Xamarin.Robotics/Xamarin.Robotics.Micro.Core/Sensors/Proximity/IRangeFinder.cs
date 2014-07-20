using System;
using Microsoft.SPOT;

namespace Xamarin.Robotics.Micro.Sensors.Proximity
{
    public interface IRangeFinder
    {
        OutputPort DistanceOutput { get; }
    }
}
