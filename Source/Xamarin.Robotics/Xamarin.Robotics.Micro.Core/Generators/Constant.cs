using System;
using Microsoft.SPOT;

namespace Xamarin.Robotics.Micro.Generators
{
    public class Constant : BlockBase
    {
        public OutputPort Output { get; private set; }

        public Constant (double value)
        {
            Output = AddOutput ("Output", Units.Scalar, value);
        }
    }
}
