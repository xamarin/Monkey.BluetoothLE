using System;
using System.Collections.Generic;
using Android.Bluetooth;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Service : IService
	{
		protected BluetoothGattService _nativeService;
		protected Device _device;

		public Service (BluetoothGattService nativeService, Device device)
		{
			this._nativeService = nativeService;
			this._device = device;
		}

		public Guid ID {
			get {
//				return this._nativeService.Uuid.ToString ();
				return Guid.ParseExact (this._nativeService.Uuid.ToString (), "d");
			}
		}

		public string Name {
			get {
				if (this._name == null)
					this._name = KnownServices.Lookup (this.ID).Name;
				return this._name;
			}
		} protected string _name = null;

		public bool IsPrimary {
			get {
				return (this._nativeService.Type == GattServiceType.Primary ? true : false);
			}
		}

		//TODO: i think this implictly requests charactersitics.
		// 
		public IList<ICharacteristic> Characteristics {
			get {
				// if it hasn't been populated yet, populate it
				if (this._characteristics == null) {
					this._characteristics = new List<ICharacteristic> ();
					foreach (var item in this._nativeService.Characteristics) {
						this._characteristics.Add (new Characteristic (item, this._device));
					}
				}
				return this._characteristics;
			}
		} protected IList<ICharacteristic> _characteristics; 

		public ICharacteristic FindCharacteristic (KnownCharacteristic characteristic)
		{
			//TODO: why don't we look in the internal list _chacateristics?
			foreach (var item in this._nativeService.Characteristics) {
				if ( string.Equals(item.Uuid.ToString(), characteristic.ID.ToString()) ) {
					return new Characteristic(item, this._device);
				}
			}
			return null;
		}

		public event EventHandler CharacteristicsDiscovered = delegate {}; // not implemented
		public void DiscoverCharacteristics()
		{

			//throw new NotImplementedException ("This is only in iOS right now, needs to be added to Android");
			this.CharacteristicsDiscovered (this, new EventArgs ());
		}
	}
}

