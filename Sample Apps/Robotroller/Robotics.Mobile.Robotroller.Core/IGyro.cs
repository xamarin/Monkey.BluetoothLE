using System;

namespace Robotics.Mobile.Robotroller
{
	public interface IGyro
	{
		double Roll { get; }
		double Pitch { get; }
		double Yaw { get; }
		event EventHandler GyroUpdated;
	}
}