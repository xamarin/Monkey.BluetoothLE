using System;

namespace Xamarin.Robotics
{
    public class ConfigPort : Port
    {
        public ConfigPort (BlockBase block, string name, Units units, double initialValue = 0)
            : base (block, name, units, initialValue)
        {
        }
    }
}

