using System;
using Robotics.Micro.SpecializedBlocks;

namespace Robotics.Micro.Sensors.Location
{
    public class Grove3AxisDigitalCompass : PollingI2CBlock
    {
        public const byte DefaultAddress = 0x1E;

        const byte CONFIGURATION_REGISTERA = 0x00;
        const byte CONFIGURATION_REGISTERB = 0x01;
        const byte MODE_REGISTER = 0x02;
        const byte DATA_REGISTER_BEGIN = 0x03;

        const byte MEASUREMENT_CONTINUOUS = 0x00;
        const byte MEASUREMENT_SINGLE_SHOT = 0x01;
        const byte MEASUREMENT_IDLE = 0x03;

        public Port XGaussOutput { get; private set; }
        public Port YGaussOutput { get; private set; }
        public Port ZGaussOutput { get; private set; }

        public Port MaxGaussInput { get; private set; }

        const double DefaultMaxGauss = 1.3;

        double scale = 0.92;

        public Grove3AxisDigitalCompass (byte address = DefaultAddress, int clockRateKhz = DefaultClockRate)
            : base (address, clockRateKhz)
        {
            XGaussOutput = AddPort ("XGaussOutput", Units.Gauss);
            YGaussOutput = AddPort ("YGaussOutput", Units.Gauss);
            ZGaussOutput = AddPort ("ZGaussOutput", Units.Gauss);

            MaxGaussInput = AddPort ("MaxGaussInput", Units.Gauss, DefaultMaxGauss);

            MaxGaussInput.ValueChanged += (s, e) => SetScale ();
            
            SetScale ();
            SetMeasurementMode (MEASUREMENT_CONTINUOUS);
        }

        byte[] readBuffer = new byte[6];

        protected override void Poll()
        {
            ReadBytes (DATA_REGISTER_BEGIN, readBuffer);

            var x = (short)(ushort)((readBuffer[0] << 8) | readBuffer[1]);
            var z = (short)(ushort)((readBuffer[2] << 8) | readBuffer[3]);
            var y = (short)(ushort)((readBuffer[4] << 8) | readBuffer[5]);

            XGaussOutput.Value = x * scale;
            ZGaussOutput.Value = z * scale;
            YGaussOutput.Value = y * scale;
        }

        void SetScale ()
        {
            var gauss = MaxGaussInput.Value;
            byte regValue = 0x00;

            if (gauss <= 0.88) {
                regValue = 0x00;
                scale = 0.73;
            }
            else if (gauss <= 1.3) {
                regValue = 0x01;
                scale = 0.92;
            }
            else if (gauss <= 1.9) {
                regValue = 0x02;
                scale = 1.22;
            }
            else if (gauss <= 2.5) {
                regValue = 0x03;
                scale = 1.52;
            }
            else if (gauss <= 4.0) {
                regValue = 0x04;
                scale = 2.27;
            }
            else if (gauss <= 4.7) {
                regValue = 0x05;
                scale = 2.56;
            }
            else if (gauss <= 5.6) {
                regValue = 0x06;
                scale = 3.03;
            }
            else {//if (gauss == 8.1) {
                regValue = 0x07;
                scale = 4.35;
            }

            // Convert from mG to Ga
            scale /= 1000;

            // Setting is in the top 3 bits of the register.
            regValue = (byte)(regValue << 5);
            WriteByte (CONFIGURATION_REGISTERB, regValue);
        }

        void SetMeasurementMode (byte mode)
        {
            WriteByte (MODE_REGISTER, mode);
        }
    }
}
