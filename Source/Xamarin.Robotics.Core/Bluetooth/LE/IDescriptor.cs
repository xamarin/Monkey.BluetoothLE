using System;

namespace Xamarin.Robotics.Core.Bluetooth.LE
{
	public interface IDescriptor
	{
		object NativeDescriptor { get; }
		Guid ID { get; }
		string Name { get; }
	}
}

