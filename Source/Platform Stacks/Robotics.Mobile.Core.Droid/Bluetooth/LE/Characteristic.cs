using System;
using System.Collections.Generic;
using System.Text;
using Android.Bluetooth;
using System.Linq;
using Java.Util;
using Android.Media;
using System.Threading.Tasks;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Characteristic : ICharacteristic
	{
		public event EventHandler<CharacteristicReadEventArgs> ValueUpdated = delegate {};
		public event EventHandler<CharacteristicWrittenEventArgs> ValueWritten = delegate {};

		protected BluetoothGattCharacteristic _nativeCharacteristic;
		protected Device _device;


		public Characteristic (BluetoothGattCharacteristic nativeCharacteristic, Device device)
		{
			this._nativeCharacteristic = nativeCharacteristic;
			this._device = device;

			if (this._device.GattCallback != null) {
				// wire up the characteristic value updating on the gattcallback
				this._device.GattCallback.CharacteristicValueUpdated += (object sender, CharacteristicReadEventArgs e) => {
					// it may be other characteristics, so we need to test
					if(e.Characteristic.ID == this.ID) {
						// update our underlying characteristic (this one will have a value)
						//TODO: is this necessary? probably the underlying reference is the same.
						//this._nativeCharacteristic = e.Characteristic;

						this.ValueUpdated (this, e);
					}
				};
			}
		}

		public string Uuid {
			get { return this._nativeCharacteristic.Uuid.ToString (); }
		}

		public Guid ID {
			get { return Guid.Parse( this._nativeCharacteristic.Uuid.ToString() ); }
		}

		public byte[] Value {
			get { return this._nativeCharacteristic.GetValue (); }
		}

		public string StringValue {
			get {
				if (this.Value == null)
					return String.Empty;
				else
					return System.Text.Encoding.UTF8.GetString (this.Value);
			}
		}

		public string Name {
			get { return KnownCharacteristics.Lookup (this.ID).Name; }
		}

		public CharacteristicPropertyType Properties {
			get {
				return (CharacteristicPropertyType)(int)this._nativeCharacteristic.Properties;
			}
		}

		public IList<IDescriptor> Descriptors {
			get {
				// if we haven't converted them to our xplat objects
				if (this._descriptors == null) {
					this._descriptors = new List<IDescriptor> ();
					// convert the internal list of them to the xplat ones
					foreach (var item in this._nativeCharacteristic.Descriptors) {
						this._descriptors.Add (new Descriptor (item));
					}
				}
				return this._descriptors;
			}
		} protected IList<IDescriptor> _descriptors;

		public object NativeCharacteristic {
			get {
				return this._nativeCharacteristic;
			}
		}

		public bool CanRead {get{return (this.Properties & CharacteristicPropertyType.Read) != 0; }}
		public bool CanUpdate {get{return (this.Properties & CharacteristicPropertyType.Notify) != 0; }}
		//NOTE: why this requires Apple, we have no idea. BLE stands for Mystery.
		public bool CanWrite {get{return (this.Properties & CharacteristicPropertyType.WriteWithoutResponse | CharacteristicPropertyType.AppleWriteWithoutResponse) != 0; }}

		public void Write (byte[] data)
		{
			if (!CanWrite) {
				throw new InvalidOperationException ("Characteristic does not support WRITE");
			}

			var c = _nativeCharacteristic;
			c.SetValue (data);
			this._device.GattCallback.CharacteristicValueWritten += this.OnWritten;
			this._device._gatt.WriteCharacteristic (c);
			Console.WriteLine(".....Write");
		}

		public void OnWritten(object sender, CharacteristicWrittenEventArgs args)
		{
			this._device.GattCallback.CharacteristicValueWritten -= this.OnWritten;
			this.ValueWritten (sender, args);
		}

		// HACK: UNTESTED - this API has only been tested on iOS
		public Task<ICharacteristic> ReadAsync()
		{
			var tcs = new TaskCompletionSource<ICharacteristic>();

			if (!CanRead) {
				throw new InvalidOperationException ("Characteristic does not support READ");
			}
			EventHandler<CharacteristicReadEventArgs> updated = null;
			updated = (object sender, CharacteristicReadEventArgs e) => {
				// it may be other characteristics, so we need to test
				var c = e.Characteristic;
				tcs.SetResult(c);
				if (this._device.GattCallback != null) {
					// wire up the characteristic value updating on the gattcallback
					this._device.GattCallback.CharacteristicValueUpdated -= updated;
				}
			};


			if (this._device.GattCallback != null) {
				// wire up the characteristic value updating on the gattcallback
				this._device.GattCallback.CharacteristicValueUpdated += updated;
			}

			Console.WriteLine(".....ReadAsync");
			this._device._gatt.ReadCharacteristic (this._nativeCharacteristic);

			return tcs.Task;
		}

		public void StartUpdates ()
		{
			// TODO: should be bool RequestValue? compare iOS API for commonality
			bool successful = false;
			if (CanRead) {
				Console.WriteLine ("Characteristic.RequestValue, PropertyType = Read, requesting updates");
				successful = this._device._gatt.ReadCharacteristic (this._nativeCharacteristic);
			}
			if (CanUpdate) {
				Console.WriteLine ("Characteristic.RequestValue, PropertyType = Notify, requesting updates");
				
				successful = this._device._gatt.SetCharacteristicNotification (this._nativeCharacteristic, true);

				// [TO20131211@1634] It seems that setting the notification above isn't enough. You have to set the NOTIFY
				// descriptor as well, otherwise the receiver will never get the updates. I just grabbed the first (and only)
				// descriptor that is associated with the characteristic, which is the NOTIFY descriptor. This seems like a really
				// odd way to do things to me, but I'm a Bluetooth newbie. Google has a example here (but ono real explaination as
				// to what is going on):
				// http://developer.android.com/guide/topics/connectivity/bluetooth-le.html#notification
				//
				// HACK: further detail, in the Forms client this only seems to work with a breakpoint on it
				// (ie. it probably needs to wait until the above 'SetCharacteristicNofication' is done before doing this...?????? [CD]
				System.Threading.Thread.Sleep(100); // HACK: did i mention this was a hack?????????? [CD] 50ms was too short, 100ms seems to work

				if (_nativeCharacteristic.Descriptors.Count > 0) {
					BluetoothGattDescriptor descriptor = _nativeCharacteristic.Descriptors [0];
					descriptor.SetValue (BluetoothGattDescriptor.EnableNotificationValue.ToArray ());
					this._device._gatt.WriteDescriptor (descriptor);
				} else {
					Console.WriteLine ("RequestValue, FAILED: _nativeCharacteristic.Descriptors was empty, not sure why");
				}
			}

			Console.WriteLine ("RequestValue, Succesful: " + successful.ToString());
		}

		public void StopUpdates ()
		{
			bool successful = false;
			if (CanUpdate) {
				successful = this._device._gatt.SetCharacteristicNotification (this._nativeCharacteristic, false);
				//TODO: determine whether 
				Console.WriteLine ("Characteristic.RequestValue, PropertyType = Notify, STOP updates");
			}
		}
	}
}

