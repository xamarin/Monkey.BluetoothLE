using System;

namespace Robotics.Micro.SpecializedBlocks
{
    public class CelsiusToFahrenheit : Block
    {
        public InputPort Celsius { get; private set; }
        public OutputPort Fahrenheit { get; private set; }

        public CelsiusToFahrenheit ()
	    {
            Celsius = AddInput ("Celsius", Units.Temperature);
            Fahrenheit = AddOutput ("Fahrenheit", Units.Scalar);

            Celsius.ValueChanged += (s, e) => {
                Fahrenheit.Value = 9.0 / 5.0 * Celsius.Value + 32;
            };
	    }
    }
}
