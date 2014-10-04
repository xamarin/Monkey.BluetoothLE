using System;

namespace Robotics.Micro.Sensors.Motion
{
	public interface IAccelerometer
	{
		OutputPort XAcceleration { get; }
		OutputPort YAcceleration { get; }
		OutputPort ZAcceleration { get; }
	}
}
