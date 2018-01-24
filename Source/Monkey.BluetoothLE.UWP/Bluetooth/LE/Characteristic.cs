using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        protected GattCharacteristicWithValue gattCharacteristicWithValue;

        public Characteristic(GattCharacteristicWithValue gattCharacteristicWithValue)
        {
            this.gattCharacteristicWithValue = gattCharacteristicWithValue;
        }

        public Characteristic(GattCharacteristic nativeCharacteristic)
        {
            this.gattCharacteristicWithValue = new GattCharacteristicWithValue(nativeCharacteristic);
        }

        public Guid ID
        {
            get { return gattCharacteristicWithValue.ID; }
        }

        public string Uuid
        {
            get { return gattCharacteristicWithValue.Uuid.ToString(); }
        }

        public byte[] Value
        {
            get { return gattCharacteristicWithValue.Value; }
        }

        public string StringValue
        {
            get { return gattCharacteristicWithValue.Value.ToString(); }
        }

        public IList<IDescriptor> Descriptors
        {
            get 
            {
                if(_descriptors == null)
                {
                    this._descriptors = new List<IDescriptor>();
                    foreach (KnownDescriptor kd in KnownDescriptors.GetDescriptors())
                    {
                        var d = gattCharacteristicWithValue.NativeCharacteristic.GetDescriptors(kd.ID)[0];
                        _descriptors.Add(new Descriptor(d));
                    }
                }
                return _descriptors;
            }
        }
        private IList<IDescriptor> _descriptors = null;

        public object NativeCharacteristic
        {
            get { return gattCharacteristicWithValue; }
        }

        public string Name
        {
            get { return KnownCharacteristics.Lookup(this.ID).Name; }
        }

        public CharacteristicPropertyType Properties
        {
            get { return (CharacteristicPropertyType)(int)this.gattCharacteristicWithValue.NativeCharacteristic.CharacteristicProperties; }
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

        public async void StartUpdates()
        {
            if (CanRead)
            {
                Debug.WriteLine("** Characteristic.RequestValue, PropertyType = Read, requesting read");

                try
                {
                    var status = await gattCharacteristicWithValue.NativeCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Characteric:StartUpdates failed: " + ex.Message);
                }
                
                if ((gattCharacteristicWithValue.NativeCharacteristic.CharacteristicProperties & GattCharacteristicProperties.Notify) == GattCharacteristicProperties.Notify)
                    gattCharacteristicWithValue.NativeCharacteristic.ValueChanged += ValueChanged;

                //should only read once
                var successful = true;
            }
            else if (CanUpdate)
            {
                Debug.WriteLine("** Characteristic.RequestValue, PropertyType = Notify, requesting updates");
                if (gattCharacteristicWithValue.NativeCharacteristic.CharacteristicProperties == GattCharacteristicProperties.Notify)
                    gattCharacteristicWithValue.NativeCharacteristic.ValueChanged += ValueChanged;
                //RegisterForUpdates();
            }
            
        }

        async Task RegisterForUpdates ()
        {
            await gattCharacteristicWithValue.NativeCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

            if (Descriptors.Count > 0)
            {
                 ValueUpdated(this, new CharacteristicReadEventArgs()
                    {
                        Characteristic = this, //we just want to send the new value
                    });
            }
            else
            {
                Debug.WriteLine("RequestValue, FAILED: _nativeCharacteristic.Descriptors was empty, not sure why");
            }
        }

        public void StopUpdates()
        {
            gattCharacteristicWithValue.NativeCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
        }

        void ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            //so here's the data
            Debug.WriteLine("Characteristic Value Changed");

            var count = args.CharacteristicValue.Length;
            byte[] buffer = new byte[count];
            var data = String.Empty;

            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(buffer);

            gattCharacteristicWithValue.Value = buffer;

            //and notify
            ValueUpdated(this, new CharacteristicReadEventArgs()
                {
                    Characteristic = this,
                });
        }

        public async Task<ICharacteristic> ReadAsync()
        {
            var val = new GattCharacteristicWithValue();
            val.NativeCharacteristic = this.gattCharacteristicWithValue.NativeCharacteristic;

            if (!CanRead)
            {
                throw new InvalidOperationException("Characteristic does not support READ");
            }

            try
            {
                GattReadResult readResult = await this.gattCharacteristicWithValue.NativeCharacteristic.ReadValueAsync();

                if (readResult.Status == GattCommunicationStatus.Success)
                {
                    val.Value = new byte[readResult.Value.Length];
                    DataReader.FromBuffer(readResult.Value).ReadBytes(val.Value);
                }
            }
            catch { }

            return new Characteristic(val);
        }

        public async void Write(byte[] data)
        {
            Debug.WriteLine("Write received:" + data.ToString());

            var dataWriter = new DataWriter();

            dataWriter.WriteBytes(data);

            var buffer = dataWriter.DetachBuffer();

            try
            {
                var status = await gattCharacteristicWithValue.NativeCharacteristic.WriteValueAsync(buffer, GattWriteOption.WriteWithoutResponse);

                if (status == GattCommunicationStatus.Success)
                    Debug.WriteLine("Write successful");
                else
                    Debug.WriteLine("Write unsuccessful");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Write unsuccessful " + ex.Message);
                
            }
           // var status = await _gattCharacteristicWithValue.NativeCharacteristic.WriteValueAsync(buffer, GattWriteOption.WriteWithoutResponse);

          //  Debug.WriteLine("Write status: " + status.ToString());
        }

        public bool CheckGattProperty (GattCharacteristicProperties gattProperty)
        {
            if (((int)gattCharacteristicWithValue.NativeCharacteristic.CharacteristicProperties & (int)gattProperty) != 0)
                return true;

            return false;
        }
    }

    //GattCharacteristic is sealed so we can't inherit 
    public class GattCharacteristicWithValue
    {
        public GattCharacteristicWithValue() { }

        public GattCharacteristicWithValue(GattCharacteristic gattCharacteristic)
        {
            NativeCharacteristic = gattCharacteristic;
        }

        public GattCharacteristic NativeCharacteristic { get; set; }

        public byte[] Value { get; set; }

        public Guid ID { get { return NativeCharacteristic.Uuid; } }

        public string Uuid { get { return NativeCharacteristic.Uuid.ToString(); } }
    }
}