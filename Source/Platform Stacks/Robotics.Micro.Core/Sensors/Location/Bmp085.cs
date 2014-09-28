using System;
using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Sensors.Location
{
    public class Bmp085 : PollingI2CBlock
    {
        public OutputPort Temperature { get; private set; }

        public const byte DefaultAddress = 0x77;

        // Calibration Data
        short ac1, ac2, ac3, ac4, ac5, ac6;
        short b1, b2;
        short mb, mc, md;
        bool needsCalibrationData = true;

        public Bmp085 (byte address = DefaultAddress, int clockRateKhz = DefaultClockRate)
            : base (address, clockRateKhz)
        {
            Temperature = AddOutput ("Temperature", Units.Temperature);
        }

        protected override void Poll ()
        {
            if (needsCalibrationData)
                ReadCalibration ();

            SetTemperature ();
        }

        void SetTemperature ()
        {
            WriteByte (REG_CONTROL, CMD_READTEMP);
            var rawTemperature = ReadInt16 (REG_TEMPDATA);

            var X1 = (rawTemperature - ac6) * (ac5) / System.Math.Pow (2, 15);
            var X2 = (mc * System.Math.Pow (2, 11)) / (X1 + md);
            var B5 = X1 + X2;
            var temp = (B5 + 8) / System.Math.Pow (2, 4);
            temp /= 10;

            Temperature.Value = temp;
        }

        void ReadCalibration ()
        {
            ac1 = ReadInt16 (REG_CAL_AC1);
            ac2 = ReadInt16 (REG_CAL_AC2);
            ac3 = ReadInt16 (REG_CAL_AC3);
            ac4 = ReadInt16 (REG_CAL_AC4);
            ac5 = ReadInt16 (REG_CAL_AC5);
            ac6 = ReadInt16 (REG_CAL_AC6);

            b1 = ReadInt16 (REG_CAL_B1);
            b2 = ReadInt16 (REG_CAL_B2);

            mb = ReadInt16 (REG_CAL_MB);
            mc = ReadInt16 (REG_CAL_MC);
            md = ReadInt16 (REG_CAL_MD);
        }

        const byte REG_CONTROL = 0xF4;
        const byte REG_TEMPDATA = 0xF6;
        const byte REG_PRESSUREDATA = 0xF6;

        const byte CMD_READTEMP = 0x2E;
        const byte CMD_READPRESSURE = 0x34;

        const byte REG_CAL_AC1 = 0xAA;
        const byte REG_CAL_AC2 = 0xAC;
        const byte REG_CAL_AC3 = 0xAE;
        const byte REG_CAL_AC4 = 0xB0;
        const byte REG_CAL_AC5 = 0xB2;
        const byte REG_CAL_AC6 = 0xB4;
        const byte REG_CAL_B1 = 0xB6;
        const byte REG_CAL_B2 = 0xB8;
        const byte REG_CAL_MB = 0xBA;
        const byte REG_CAL_MC = 0xBC;
        const byte REG_CAL_MD = 0xBE;
    }
}
