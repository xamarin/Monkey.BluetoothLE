using System;

namespace Robotics.Micro
{
    public class ConfigPort : Port
    {
        public ConfigPort (Block block, string name, Units units, double initialValue = 0)
            : base (block, name, units, initialValue)
        {
        }
    }
}

