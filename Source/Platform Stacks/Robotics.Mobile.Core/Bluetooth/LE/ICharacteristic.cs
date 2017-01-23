using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public interface ICharacteristic
	{
		// events
		event EventHandler<CharacteristicReadEventArgs> ValueUpdated;

		// properties
		Guid ID { get; }
		string Uuid { get; }
		byte[] Value { get; }
		string StringValue { get; }
		IList<IDescriptor> Descriptors { get; }
		object NativeCharacteristic { get; }
		string Name { get; }
		CharacteristicPropertyType Properties { get; }

		bool CanRead { get; }
		bool CanUpdate { get; }
		bool CanWrite { get; }

		// methods
//		void EnumerateDescriptors ();

		void StartUpdates();
		void StopUpdates();

		Task<ICharacteristic> ReadAsync ();

		void Write (byte[] data);

		/// <summary>
		/// Write a chunck of data to the property and wait for the device callback before returning
		/// </summary>
		/// <returns> returns yes if the write as been succesfully done </returns>
		/// <param name="data">Data.</param>
		/// TODO: Maybe add a timeout
		Task<bool> WriteAsync(byte[] data); 

	}
}

