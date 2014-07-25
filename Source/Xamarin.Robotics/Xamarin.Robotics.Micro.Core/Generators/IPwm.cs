using System;
using Microsoft.SPOT;

namespace Xamarin.Robotics.Micro.Generators
{
    public interface IPwm
    {
        InputPort DutyCycleInput { get; }
        InputPort FrequencyInput { get; }
    }
}
