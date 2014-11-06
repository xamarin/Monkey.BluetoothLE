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
                if (_characteristcs == null)
                {
                    this._characteristcs = new List<ICharacteristic>();


                    //TODO not able to read characteristics based on the UUID
                    //_nativeService.C

                    foreach (GattCharacteristic c in this._nativeService.GetCharacteristics(GattCharacteristicUuids.BatteryLevel))
                    {
                        this._characteristcs.Add(new Characteristic(c));
                    }     

                    foreach (GattCharacteristic c in this._nativeService.GetCharacteristics(_nativeService.Uuid))
                    {
                        this._characteristcs.Add(new Characteristic(c));
                    }     
                }
                return _characteristcs;
            }
        }
        protected IList<ICharacteristic> _characteristcs; 

        public ICharacteristic FindCharacteristic(KnownCharacteristic characteristic)
        {
            throw new NotImplementedException();
        }

        public void DiscoverCharacteristics()
        {
            this.CharacteristicsDiscovered(this, new EventArgs());
        }

        Guid ExtractGuid(string id)
        {
            //this is wrong
            int start = id.IndexOf('{') + 1;

            var guid = id.Substring(start, 36);

            return Guid.Parse(guid);

        }
    }
}
