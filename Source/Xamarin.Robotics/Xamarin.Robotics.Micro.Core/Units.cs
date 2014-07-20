using System;

namespace Xamarin.Robotics
{
	public enum Units
    {
        /// <summary>
        /// This value has no units and is just a number.
        /// </summary>
        Scalar = 0,

        /// <summary>
        /// A value from 0 to 1 inclusive.
        /// </summary>
        Ratio,

        /// <summary>
        /// Only values 0 (low) and 1 (high).
        /// </summary>
        Digital,

        /// <summary>
        /// Measured in Meters.
        /// </summary>
        Distance,

        /// <summary>
        /// Measured in Hertz.
        /// </summary>
        Frequency,

        /// <summary>
        /// Measured in Seconds.
        /// </summary>
        Time,

        Temperature,

        EarthGravity,

        Gauss,

        Resistance,
    }

    public static class UnitsEx
    {
        public static string ToShortString (this Units units)
        {
            switch (units) {
            case Units.Scalar:
                return "";
            case Units.Ratio:
                return "";
            case Units.Digital:
                return "";
            case Units.Frequency:
                return "Hz";
            case Units.Time:
                return "s";
            case Units.Temperature:
                return "\u00BAC";
            case Units.EarthGravity:
                return "g";
            case Units.Gauss:
                return "gauss";
            case Units.Resistance:
                return "\u2126";
            default:
                return "";
            }
        }
    }
}
