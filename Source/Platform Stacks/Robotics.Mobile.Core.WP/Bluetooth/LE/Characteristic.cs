using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Characteristic : ICharacteristic
    {
        public event EventHandler<CharacteristicReadEventArgs> ValueUpdated;

        protected GattCharacteristic _nativeCharacteristic;

        public Characteristic(GattCharacteristic nativeCharacteristic)
        {
            this._nativeCharacteristic = nativeCharacteristic;
        }

        public Guid ID
        {
            get { return _nativeCharacteristic.Uuid; }
        }

        public string Uuid
        {
            get { return _nativeCharacteristic.Uuid.ToString(); }
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
            get 
            {
                if(_descriptors == null)
                {
                    foreach (KnownDescriptor kd in KnownDescriptors.GetDescriptors())
                    {
                        var d = _nativeCharacteristic.GetDescriptors(kd.ID)[0];
                        _descriptors.Add(new Descriptor(d));
                    }
                }
                return _descriptors;
            }
        }
        private IList<IDescriptor> _descriptors = null;

        public object NativeCharacteristic
        {
            get { return _nativeCharacteristic; }
        }

        public string Name
        {
            get { return KnownCharacteristics.Lookup(this.ID).Name; }
        }

        public CharacteristicPropertyType Properties
        {
            get { return (CharacteristicPropertyType)(int)this._nativeCharacteristic.CharacteristicProperties; }
        }

        public bool CanRead
        {
            get 
            {
                if (CheckGattProperty(GattCharacteristicProperties.Read))
                    return true;
                return false;
            }
        }

        public bool CanUpdate
        {
            get
            {
                if (CheckGattProperty(GattCharacteristicProperties.Notify))
                    return true;
                return false;
            }
        }

        public bool CanWrite
        {
            get {
                if (CheckGattProperty(GattCharacteristicProperties.Write) || CheckGattProperty(GattCharacteristicProperties.WriteWithoutResponse))
                    return true;
                return false;
            }
        }

        public void StartUpdates()
        {
            bool successful = false;
            if (CanRead)
            {
                Console.WriteLine("** Characteristic.RequestValue, PropertyType = Read, requesting read");
                _nativeCharacteristic.ValueChanged += ValueChanged;

                   

                successful = true;
            }
            if (CanUpdate)
            {
                Console.WriteLine("** Characteristic.RequestValue, PropertyType = Notify, requesting updates");

                RegisterForUpdates();
            }
        }

        async Task RegisterForUpdates ()
        {
            await this._nativeCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

            if (this.Descriptors.Count > 0)
            {


            }
            else
            {
                Console.WriteLine("RequestValue, FAILED: _nativeCharacteristic.Descriptors was empty, not sure why");
            }

            //successful = true;

        }

        public void StopUpdates()
        {
            this._nativeCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                
        }

        void ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            
        }

        public Task<ICharacteristic> ReadAsync()
        {
            var tcs = new TaskCompletionSource<ICharacteristic>();

            if (!CanRead)
            {
                throw new InvalidOperationException("Characteristic does not support READ");
            }

            throw new NotImplementedException();
        }

        public void Write(byte[] data)
        {
            var dataWriter = new DataWriter();

            dataWriter.WriteBytes(data);

            var buffer = dataWriter.DetachBuffer();

            _nativeCharacteristic.WriteValueAsync(buffer, GattWriteOption.WriteWithoutResponse);
        }

        public bool CheckGattProperty (GattCharacteristicProperties gattProperty)
        {
            if(((int)_nativeCharacteristic.CharacteristicProperties & (int)gattProperty) != 0)
            {
                return true;
            }
            return false;
        }
    }
}
