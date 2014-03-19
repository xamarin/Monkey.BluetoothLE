using System;
using System.Linq;
using MonoTouch.CoreBluetooth;
using System.Collections.Generic;

namespace Xamarin.Robotics.Core.Bluetooth.LE
{
	public class Characteristic : ICharacteristic
	{
		public event EventHandler<CharacteristicReadEventArgs> ValueUpdated = delegate {};

		protected CBCharacteristic _nativeCharacteristic;

		public Characteristic (CBCharacteristic nativeCharacteristic)
		{
			this._nativeCharacteristic = nativeCharacteristic;
		}
		public string Uuid {
			get { return this._nativeCharacteristic.UUID.ToString (); }
		}

		public Guid ID {
			get { return CharacteristicUuidToGuid (this._nativeCharacteristic.UUID); }
		}

		public byte[] Value {
			get { return this._nativeCharacteristic.Value.ToArray(); }
		}

		public string StringValue {
			get {
				if (this.Value == null)
					return String.Empty;
				else
					return System.Text.Encoding.UTF8.GetString (this.Value);
			}
		}

		public string Name {
			get { return KnownCharacteristics.Lookup (this.ID).Name; }
		}

		public CharacteristicPropertyType Properties {
			get {
				return (CharacteristicPropertyType)(int)this._nativeCharacteristic.Properties;
			}
		}

		public IList<IDescriptor> Descriptors {
			get {
				// if we haven't converted them to our xplat objects
				if (this._descriptors != null) {
					this._descriptors = new List<IDescriptor> ();
					// convert the internal list of them to the xplat ones
					foreach (var item in this._nativeCharacteristic.Descriptors) {
						this._descriptors.Add (new Descriptor (item));
					}
				}
				return this._descriptors;
			}
		} protected IList<IDescriptor> _descriptors;

		public object NativeCharacteristic {
			get {
				return this._nativeCharacteristic;
			}
		}

		public void RequestValue ()
		{
			// TODO: should be bool RequestValue? compare iOS API for commonality
			bool successful = false;
			if((this.Properties & CharacteristicPropertyType.Read) != 0) {
				Console.WriteLine ("Characteristic.RequestValue, PropertyType = Read, requesting updates");
				successful = true;
			
				this.ValueUpdated (this, new CharacteristicReadEventArgs () { });
			}
			if ((this.Properties & CharacteristicPropertyType.Notify) != 0) {
				Console.WriteLine ("Characteristic.RequestValue, PropertyType = Notify, requesting updates");

				//TODO: need to call SetNotifyValue() on the CBPeripheral
				//successful = 
			}

			Console.WriteLine ("RequestValue, Succesful: " + successful.ToString());
		}

		//TODO: this is the exact same as ServiceUuid i think
		public static Guid CharacteristicUuidToGuid ( CBUUID uuid)
		{
			//this sometimes returns only the significant bits, e.g.
			//180d or whatever. so we need to add the full string
			string id = uuid.ToString ();
			if (id.Length == 4) {
				id = "0000" + id + "-0000-1000-8000-00805f9b34fb";
			}
			return Guid.ParseExact (id, "d");
		}


	}
}

