using System;
using System.Collections.Generic;

namespace Xamarin.Robotics.Core.Bluetooth.LE
{
	public interface IService
	{
		Guid ID { get; }
		String Name { get; }
		bool IsPrimary { get; } // iOS only?
		IList<ICharacteristic> Characteristics { get; }
		ICharacteristic FindCharacteristic (KnownCharacteristic characteristic);
		//IDictionary<Guid, ICharacteristic> Characteristics { get; }
	}
}