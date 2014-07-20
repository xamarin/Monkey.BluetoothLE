using System;

namespace Xamarin.Robotics.Micro.Sensors.Temperature
{
    public class Thermistor : BlockBase
    {
        /// <summary>
        /// Value that ranges from 0 to 1.
        /// </summary>
        public InputPort AnalogInput { get; private set; }

        /// <summary>
        /// Measured temperature.
        /// </summary>
        public OutputPort Temperature { get; private set; }

        /// <summary>
        /// The maximum value that AnalogInput can attain. For a
        /// 10-bit ADC, this is 1023.
        /// </summary>
        public ConfigPort AnalogMaximum { get; private set; }

        /// <summary>
        /// Resistance of the resistor placed in series with the Thermistor.
        /// </summary>
        public ConfigPort PullupResistance { get; private set; }

        /// <summary>
        /// Steinhart-Hart A parameter.
        /// </summary>
        public ConfigPort CalibrationA { get; private set; }

        /// <summary>
        /// Steinhart-Hart B parameter.
        /// </summary>
        public ConfigPort CalibrationB { get; private set; }

        /// <summary>
        /// Steinhart-Hart C parameter.
        /// </summary>
        public ConfigPort CalibrationC { get; private set; }

		public Thermistor ()
        {
            AnalogInput = AddInput ("AnalogInput", Units.Ratio, 0);
            Temperature = AddOutput ("Temperature", Units.Temperature);

            PullupResistance = AddConfig ("PullupResistance", Units.Resistance, 10000);

            CalibrationA = AddConfig ("CalibrationA", Units.Scalar, 0.001129148);
            CalibrationB = AddConfig ("CalibrationB", Units.Scalar, 0.000234125);
            CalibrationC = AddConfig ("CalibrationC", Units.Scalar, 0.0000000876741);

            SetTemperature ();

            AnalogInput.ValueChanged += (s, e) => SetTemperature ();
        }

        void SetTemperature ()
        {
            var eps = 1.0e-6;

            // R => P*A/(-A + M)
            // M = 1

            // Make sure the max is positive
            var analog = System.Math.Max (AnalogInput.Value, 0);

            var thermistorResistance = System.Math.Max (PullupResistance.Value * analog / (1 - analog), eps);

            var logR = System.Math.Log (thermistorResistance);

            var oneOverKelvin = CalibrationA.Value +
                CalibrationB.Value * logR +
                CalibrationC.Value * logR * logR * logR;

            var kelvin = oneOverKelvin > 0 ? 1 / oneOverKelvin : 1 / eps;

            var celsius = kelvin - 270;

            Temperature.Value = celsius;
        }
    }
}
