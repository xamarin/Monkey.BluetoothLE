using System;
using Microsoft.SPOT;

namespace Xamarin.Robotics.Motors
{
    public interface IDCMotor
    {
        /// <summary>
        /// Direction of the motor.
        /// Negative values &lt; -0.5 are reverse.
        /// Positive values &gt; 0.5 are forwards.
        /// Others are neutral.
        /// </summary>
        InputPort DirectionInput { get; }

        /// <summary>
        /// The speed of the motor from 0 to 1.
        /// </summary>
        InputPort SpeedInput { get; }
    }
}
