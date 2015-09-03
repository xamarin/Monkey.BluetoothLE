using System;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class CharacteristicReadEventArgs : EventArgs
	{
		public ICharacteristic Characteristic { get; set; }

		public CharacteristicReadEventArgs ()
		{
		}

		public override string ToString ()
		{
			if (this.Characteristic == null)
				return string.Format ("[CharacteristicReadEventArgs]");
			string bytes = "[";
			if (this.Characteristic != null && this.Characteristic.Value != null) {
				foreach (var b in this.Characteristic.Value) {
					bytes += b + ",";
				}
			}
			if (bytes.Length > 1)
				bytes = bytes.Substring (0, bytes.Length - 1) + "]";
			else
				bytes += "]";
			
			return string.Format ("[CharacteristicReadEventArgs: Id={0}, bytes:{1}]", this.Characteristic.ID, bytes);
		}
	}
}

