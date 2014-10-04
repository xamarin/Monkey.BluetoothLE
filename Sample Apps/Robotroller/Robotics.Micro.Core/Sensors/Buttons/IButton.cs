using System;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
#endif

namespace Robotics.Micro.Sensors.Buttons
{
	public interface IButton
	{
		event EventHandler Clicked;

	}
}
