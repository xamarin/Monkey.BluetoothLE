using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Characteristic : ICharacteristic
    {
        public event EventHandler<CharacteristicReadEventArgs> ValueUpdated;

        public Guid ID
        {
            get { throw new NotImplementedException(); }
        }

        public string Uuid
        {
            get { throw new NotImplementedException(); }
        }

        public byte[] Value
        {
            get { throw new NotImplementedException(); }
        }

        public string StringValue
        {
            get { throw new NotImplementedException(); }
        }

        public IList<IDescriptor> Descriptors
        {
            get { throw new NotImplementedException(); }
        }

        public object NativeCharacteristic
        {
            get { throw new NotImplementedException(); }
        }

        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public CharacteristicPropertyType Properties
        {
            get { throw new NotImplementedException(); }
        }

        public bool CanRead
        {
            get { throw new NotImplementedException(); }
        }

        public bool CanUpdate
        {
            get { throw new NotImplementedException(); }
        }

        public bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public void StartUpdates()
        {
            throw new NotImplementedException();
        }

        public void StopUpdates()
        {
            throw new NotImplementedException();
        }

        public Task<ICharacteristic> ReadAsync()
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
