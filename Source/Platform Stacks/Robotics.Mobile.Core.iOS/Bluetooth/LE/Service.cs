using System;
using System.Collections.Generic;

#if __UNIFIED__
using CoreBluetooth;
#else
using MonoTouch.CoreBluetooth;
#endif

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Service : IService
	{
		public event EventHandler CharacteristicsDiscovered = delegate {};

		protected CBService _nativeService;
		protected CBPeripheral _parentDevice;

		public Service (CBService nativeService, CBPeripheral parentDevice )
		{
			this._nativeService = nativeService;
			this._parentDevice = parentDevice;


		}

		public Guid ID {
			get {
				return ServiceUuidToGuid (this._nativeService.UUID);
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
				return this._nativeService.Primary;
			}
		}

		//TODO: decide how to Interface this, right now it's only in the iOS implementation
		public void DiscoverCharacteristics()
		{
			// TODO: need to raise the event and listen for it.
			this._parentDevice.DiscoverCharacteristics ( this._nativeService );
		}

		public IList<ICharacteristic> Characteristics {
			get {
				// if it hasn't been populated yet, populate it
				if (this._characteristics == null) {
					this._characteristics = new List<ICharacteristic> ();
					if (this._nativeService.Characteristics != null) {
						foreach (var item in this._nativeService.Characteristics) {
							this._characteristics.Add (new Characteristic (item, _parentDevice));
						}
					}
				}
				return this._characteristics;
			}
		} protected IList<ICharacteristic> _characteristics;

		public void OnCharacteristicsDiscovered()
		{
			this.CharacteristicsDiscovered (this, new EventArgs ());
		}

		public ICharacteristic FindCharacteristic (KnownCharacteristic characteristic)
		{
			//TODO: why don't we look in the internal list _chacateristics?
			foreach (var item in this._nativeService.Characteristics) {
				if ( string.Equals(item.UUID.ToString(), characteristic.ID.ToString()) ) {
					return new Characteristic(item, _parentDevice);
				}
			}
			return null;
		}

		public static Guid ServiceUuidToGuid ( CBUUID uuid)
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

