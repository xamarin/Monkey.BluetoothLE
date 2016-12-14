using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Descriptor : IDescriptor
    {
        public object NativeDescriptor
        {
            get { return this._nativeDescriptor as Object;  }
        } protected GattDescriptor _nativeDescriptor;

        public Guid ID
        {
            get { return _nativeDescriptor.Uuid; }
        }

        public string Name
        {
            get
            {
                if (this._name == null)
                    this._name = KnownDescriptors.Lookup(this.ID).Name;
                return this._name;
            }
        } protected string _name = null;

        public Descriptor(GattDescriptor nativeDescriptor)
        {
            this._nativeDescriptor = nativeDescriptor;
        }
    }
}
