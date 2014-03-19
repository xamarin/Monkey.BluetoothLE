using System;

namespace Xamarin.Robotics.Sensors.Motion
{
	public interface IAccelerometer
	{
		OutputPort XAcceleration { get; }
		OutputPort YAcceleration { get; }
		OutputPort ZAcceleration { get; }
	}
}
