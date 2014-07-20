using System;
using Microsoft.SPOT;

namespace Xamarin.Robotics.Motors
{
    public interface IDCMotor
    {
        /// <summary>
        /// The speed of the motor from -1 to 1.
        /// </summary>
        InputPort SpeedInput { get; }
    }
}
