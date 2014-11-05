using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            get { return ExtractGuid(this._nativeService.DeviceId); }
        }

        public string Name
        {
            get { return this._nativeService.DeviceId; }
        }

        public bool IsPrimary
        {
            get { throw new NotImplementedException(); }
        }

        public IList<ICharacteristic> Characteristics
        {
            get { throw new NotImplementedException(); }
        }

        public ICharacteristic FindCharacteristic(KnownCharacteristic characteristic)
        {
            throw new NotImplementedException();
        }

        public void DiscoverCharacteristics()
        {
            throw new NotImplementedException();
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
