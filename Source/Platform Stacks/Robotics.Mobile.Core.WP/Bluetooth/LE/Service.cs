using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Service : IService
    {
        public event EventHandler CharacteristicsDiscovered;

        protected GattDeviceService _nativeService;

        public Service(GattDeviceService nativeService)
        {
            this._nativeService = nativeService;
        }

        public Guid ID
        {
            get
            {
                if(this._ID == Guid.Empty)
                    this._ID = ExtractGuid(this._nativeService.DeviceId);
                return _ID;
            }
        }
        protected Guid _ID = Guid.Empty;

        public string Name
        {
            get
            {
                if (this._name == null)
                    this._name = KnownServices.Lookup(this.ID).Name;
                return this._name;
            }
        }
        protected string _name = null;

        public bool IsPrimary
        {
            get { throw new NotImplementedException(); }
        }

        public IList<ICharacteristic> Characteristics
        {
            get
            {
                if (_characteristics == null)
                {
                    this._characteristics = new List<ICharacteristic>();

                    //TODO - there doesn't appear to be any way to search for characteristics using the WP Silverlight API
                    //so we're going to request the specific characteristics for now
                    KnownCharacteristics.LoadItemsFromJson();

                    foreach (KnownCharacteristic kc in KnownCharacteristics.GetCharacteristics())
                    {
                        var c = this._nativeService.GetCharacteristics(kc.ID).FirstOrDefault();
                        if (c != null)
                            this._characteristics.Add(new Characteristic(c));
                    }
                }
                return _characteristics;
            }
        }
        protected IList<ICharacteristic> _characteristics; 

        public ICharacteristic FindCharacteristic(KnownCharacteristic characteristic)
        {
            var c = this._nativeService.GetCharacteristics(characteristic.ID).FirstOrDefault();

            return new Characteristic(c);
        }

        public void DiscoverCharacteristics()
        {
            if (Characteristics != null && Characteristics.Count > 0)
            {
                this.CharacteristicsDiscovered(this, new EventArgs());
            }
        }

        Guid ExtractGuid(string id)
        {
            int start = id.IndexOf('{') + 1;

            var guid = id.Substring(start, 36);

            return Guid.Parse(guid);
        }
    }
}
