using System;

namespace Robotics.Micro.Generators
{
    public interface IPwm
    {
        InputPort DutyCycleInput { get; }
        InputPort FrequencyInput { get; }
    }
}
