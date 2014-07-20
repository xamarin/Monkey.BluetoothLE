using System;

namespace Xamarin.Robotics.Micro
{
	public class OutputPort : Port
	{
//		readonly TUnits units;

		public OutputPort (BlockBase block, string name, Units units, double initialValue)
			: base (block, name, units, initialValue)
		{
//			units = new TUnits ();
		}
	}
}

