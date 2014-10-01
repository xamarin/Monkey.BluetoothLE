using System;

#if MF_FRAMEWORK_VERSION_V4_3
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
#endif

namespace Robotics.Micro.SpecializedBlocks
{
    public abstract class I2CBlock : Block
    {
        public const int DefaultClockRate = 400;
        const int TransactionTimeout = 1000;

		#if MF_FRAMEWORK_VERSION_V4_3
        I2CDevice.Configuration i2cConfig;
        I2CDevice i2cDevice;
		#endif

        public byte Address { get; private set; }

        readonly int clockRateKhz;

        protected I2CBlock (byte address, int clockRateKhz = DefaultClockRate)
        {
            this.Address = address;
            this.clockRateKhz = clockRateKhz;            
        }

        protected void WriteByte (byte register, byte value)
        {
            this.Write (new byte[] { register, value });
        }
        protected void WriteBytes (byte register, byte[] values)
        {
            byte[] writeBuffer = new byte[values.Length + 1];
            writeBuffer[0] = register;
            Array.Copy (values, 0, writeBuffer, 1, values.Length);
            Write (writeBuffer);
        }
        protected void ReadBytes (byte register, byte[] readBuffer)
        {
            Write (new byte[] { register });
            Read (readBuffer);
        }
        protected byte ReadByte (byte register)
        {
            Write (new byte[] { register });
            var readBuffer = new byte[1];
            Read (readBuffer);
            return readBuffer[0];
        }
        protected short ReadInt16 (byte register)
        {
            Write (new byte[] { register });
            var readBuffer = new byte[2];
            Read (readBuffer);
			return (short)(ushort)((readBuffer[0] << 8) | readBuffer[1]);
        }

		#if MF_FRAMEWORK_VERSION_V4_3

        void Connect ()
        {
            if (i2cDevice != null)
                return;

            this.i2cConfig = new I2CDevice.Configuration (this.Address, clockRateKhz);
            this.i2cDevice = new I2CDevice (this.i2cConfig);
        }

        void Write (byte[] writeBuffer)
        {
            Connect ();

            // create a write transaction containing the bytes to be written to the device
            var writeTransaction = new I2CDevice.I2CTransaction[] {
                I2CDevice.CreateWriteTransaction(writeBuffer)
            };

            // write the data to the device
            int written = this.i2cDevice.Execute (writeTransaction, TransactionTimeout);

            while (written < writeBuffer.Length) {
                byte[] newBuffer = new byte[writeBuffer.Length - written];
                Array.Copy (writeBuffer, written, newBuffer, 0, newBuffer.Length);

                writeTransaction = new I2CDevice.I2CTransaction[]
	            {
	                I2CDevice.CreateWriteTransaction(newBuffer)
	            };

                written += this.i2cDevice.Execute (writeTransaction, TransactionTimeout);
            }

            // make sure the data was sent
            if (written != writeBuffer.Length) {
                throw new Exception ("Could not write to device.");
            }
        }

        void Read (byte[] readBuffer)
        {
            Connect ();

            // create a read transaction
            var readTransaction = new I2CDevice.I2CTransaction[] {
                I2CDevice.CreateReadTransaction(readBuffer)
            };

            // read data from the device
            int read = this.i2cDevice.Execute (readTransaction, TransactionTimeout);

            // make sure the data was read
            if (read != readBuffer.Length) {
                throw new Exception ("Could not read from device.");
            }
        }

		#else

		void Connect ()
		{
			throw new NotSupportedException ();
		}

		void Read (byte[] readBuffer)
		{
			throw new NotSupportedException ();
		}

		void Write (byte[] writeBuffer)
		{
			throw new NotSupportedException ();
		}

		#endif
    }
}
