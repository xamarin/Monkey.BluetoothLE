using System;
using Microsoft.SPOT;

namespace Xamarin.Robotics.Sensors.Proximity
{
    public interface IRangeFinder
    {
        OutputPort DistanceOutput { get; }
    }
}
