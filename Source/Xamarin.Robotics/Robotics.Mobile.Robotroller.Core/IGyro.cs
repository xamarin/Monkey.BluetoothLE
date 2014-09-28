using System;

namespace Xamarin.Robotics.Mobile.Robotroller
{
	public interface IGyro
	{
		double Roll { get; }
		double Pitch { get; }
		double Yaw { get; }
		event EventHandler GyroUpdated;
	}
}