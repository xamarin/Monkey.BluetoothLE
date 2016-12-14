using System;
using CoreBluetooth;
using System.Collections.Generic;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Service : IService
	{
		public event EventHandler CharacteristicsDiscovered = delegate {};

		protected CBService nativeService;
		protected CBPeripheral parentDevice;

		public Service (CBService nativeService, CBPeripheral parentDevice )
		{
			this.nativeService = nativeService;
			this.parentDevice = parentDevice;
		}

		public Guid ID { get { return ServiceUuidToGuid (this.nativeService.UUID); } }

		public string Name
        {
			get
            {
				if (name == null)
					name = KnownServices.Lookup (this.ID).Name;
				return name;
			}
		} protected string name = null;

		public bool IsPrimary {	get { return nativeService.Primary; }	}

		//TODO: decide how to Interface this, right now it's only in the iOS implementation
		public void DiscoverCharacteristics()
		{
			// TODO: need to raise the event and listen for it.
			this.parentDevice.DiscoverCharacteristics ( this.nativeService );
		}

		public IList<ICharacteristic> Characteristics {
			get {
				// if it hasn't been populated yet, populate it
				if (characteristics == null)
                {
					characteristics = new List<ICharacteristic> ();

					if (nativeService.Characteristics != null)
                    {
						foreach (var item in this.nativeService.Characteristics) 
							characteristics.Add (new Characteristic (item, parentDevice));
					}
				}
				return this.characteristics;
			}
		} protected IList<ICharacteristic> characteristics;

		public void OnCharacteristicsDiscovered()
		{
			CharacteristicsDiscovered (this, new EventArgs ());
		}

		public ICharacteristic FindCharacteristic (KnownCharacteristic characteristic)
		{
			//TODO: why don't we look in the internal list _chacateristics?
			foreach (var item in this.nativeService.Characteristics)
            {
				if ( string.Equals(item.UUID.ToString(), characteristic.ID.ToString()) ) {
					return new Characteristic(item, parentDevice);
				}
			}
			return null;
		}

		public static Guid ServiceUuidToGuid ( CBUUID uuid)
		{
			//this sometimes returns only the significant bits, e.g.
			//180d or whatever. so we need to add the full string
			string id = uuid.ToString ();

            if (id.Length == 4) 
				id = "0000" + id + "-0000-1000-8000-00805f9b34fb";
			
			return Guid.ParseExact (id, "d");
		}
	}
}