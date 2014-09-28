using System;
using System.Collections;
using System.Threading;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
#endif

namespace Robotics.Micro
{
	public interface IScope
    {
		void Connect (Port port);
    }
}
