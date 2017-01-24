using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Threading;
using System.IO;
using Robotics.Mobile.Core.Bluetooth.LE.Interfaces;
using Robotics.Mobile.Core.Bluetooth.LE.LeTypeIds;
using System.Diagnostics;

namespace Robotics.Mobile.Core.Bluetooth.LE {

    /// <summary>Wrap the Bluetooth LE read write characteristics in a stream</summary>
    public class BluetoothLEStream : Stream {

        #region Data

        const string CLASS = "BluetoothLEStream";

        private readonly Task initTask;
        private readonly IDevice device;
        private IService service;
        private ICharacteristic receive;
        private ICharacteristic transmit;
        private ICharacteristic mode;

        const int ReadBufferSize = 64 * 1024;
        readonly List<byte> readBuffer = new List<byte>(ReadBufferSize * 2);
        readonly AutoResetEvent dataReceived = new AutoResetEvent(false);

        IBluetoothLE_TypeSet idTypes = null;

        byte[] modeValue = new byte[] { 1 }; // Data mode, 2= command mode

        #endregion

        /// <summary>Alternate event you can use to listen for incoming reads</summary>
        public event Action<byte[]> OnRead;

        #region Properties

        public IDevice CurrentDevice {
            get {
                return this.device;
            }
        }

        #endregion

        #region Constructors

        public BluetoothLEStream(IDevice device, IBluetoothLE_TypeSet idTypes) {
            this.device = device;
            this.idTypes = idTypes;
            this.initTask = InitializeAsync();
        }

        #endregion

        public void CancelRead() {
            this.receive.StopUpdates();
            this.receive.ValueUpdated -= this.HandleReceiveValueUpdated;
            this.dataReceived.Set();
        }

        #region implemented abstract members of Stream

        public override int Read(byte[] buffer, int offset, int count) {
            Task<int> t = ReadAsync(buffer, offset, count, CancellationToken.None);
            t.Wait();
            return t.Result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            await initTask;
            while (!cancellationToken.IsCancellationRequested) {
                lock (this.readBuffer) {
                    if (this.readBuffer.Count > 0) {
                        int n = Math.Min(count, readBuffer.Count);
                        this.readBuffer.CopyTo(0, buffer, offset, n);
                        this.readBuffer.RemoveRange(0, n);
                        return n;
                    }
                }
                await Task.Run(() => this.dataReceived.WaitOne());
            }
            return 0;
        }

        public override void Write(byte[] buffer, int offset, int count) {
            WriteAsync(buffer, offset, count).Wait();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            if (count > 20) {
                throw new ArgumentOutOfRangeException("count", "This function is limited to buffers of 20 bytes and less.");
            }

            await initTask;

            var b = buffer;
            if (offset != 0 || count != b.Length) {
                b = new byte[count];
                Array.Copy(buffer, offset, b, 0, count);
            }

            // Write the data
            transmit.Write(b);

            // Throttle
            await Task.Delay(TimeSpan.FromMilliseconds(b.Length)); // 1 ms/byte is slow but reliable
        }

        public override void Flush() {
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }
        public override void SetLength(long value) {
            throw new NotSupportedException();
        }
        public override bool CanRead {
            get {
                return true;
            }
        }
        public override bool CanSeek {
            get {
                return false;
            }
        }
        public override bool CanWrite {
            get {
                return true;
            }
        }
        public override long Length {
            get {
                return 0;
            }
        }
        public override long Position {
            get {
                return 0;
            }
            set {
            }
        }
        #endregion

        #region Private methods

        private async Task InitializeAsync() {
            Debug.WriteLine("LEStream: Looking for service " + this.idTypes.GetId(BluetoothLE_IdType.Service) + "...");
            this.service = await device.GetServiceAsync(this.idTypes.GetId(BluetoothLE_IdType.Service));
            Debug.WriteLine("LEStream: Got service: " + service.ID);

            Debug.WriteLine("LEStream: Getting characteristics...");
            this.receive = await service.GetCharacteristicAsync(this.idTypes.GetId(BluetoothLE_IdType.ReceiveCharacteristic));
            this.transmit = await service.GetCharacteristicAsync(this.idTypes.GetId(BluetoothLE_IdType.TransmitCharacteristic));
            this.mode = await this.service.GetCharacteristicAsync(this.idTypes.GetId(BluetoothLE_IdType.ModeCharacteristic));
            Debug.WriteLine("LEStream: Got characteristics");

            this.SetMode();

            // Set the receive thread going
            this.receive.ValueUpdated += this.HandleReceiveValueUpdated;
            this.receive.StartUpdates();
        }


        /// <summary>Set Bluetooth Serial Port Mode On</summary>
        /// <remarks>TODO - modify to pass in data or command mode enums</remarks>
        private void SetMode() {
            this.mode.Write(this.modeValue);
        }


        private void HandleReceiveValueUpdated(object sender, CharacteristicReadEventArgs e) {
            //WrapErr.ToErrReport(1234, "Failed to receive value", () => {
                byte[] bytes = e.Characteristic.Value;
                if (bytes == null || bytes.Length == 0) {
                    return;
                }

                // Instead of an async wait on ReadAsync you can just subscribe to the 
                this.RaiseMessage(e.Characteristic.Value);

                // Original way of returning data via the ReadAsync
                //Log.Info(CLASS, "HandleReceiveValueUpdated", () => string.Format("Receive.Value: {0} - {1}", 
                //    ByteTools.ToPrintableBytes(bytes), ByteTools.ToPrintableString(bytes)));
                lock (this.readBuffer) {
                    if (this.readBuffer.Count + bytes.Length > ReadBufferSize) {
                        this.readBuffer.RemoveRange(0, ReadBufferSize / 2);
                    }
                    this.readBuffer.AddRange(bytes);
                }
                this.dataReceived.Set();
            //});
        }



        #endregion


        private void RaiseMessage(byte[] message) {
            if (this.OnRead != null) {
                Task.Factory.StartNew(() => {
                    try {
                        this.OnRead(message);
                    }
                    catch (Exception e) {
                        Debug.WriteLine(e.Message);
                    }
                });
            }
            else {
                Debug.WriteLine("No subscribers to OnRead");
            }
        }


    }



}