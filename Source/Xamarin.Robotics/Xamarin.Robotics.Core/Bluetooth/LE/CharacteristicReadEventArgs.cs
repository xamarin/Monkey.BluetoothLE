using System;

namespace Xamarin.Robotics.Core.Bluetooth.LE
{
	public class CharacteristicReadEventArgs : EventArgs
	{
		public ICharacteristic Characteristic { get; set; }

		public CharacteristicReadEventArgs ()
		{
		}
	}
}

