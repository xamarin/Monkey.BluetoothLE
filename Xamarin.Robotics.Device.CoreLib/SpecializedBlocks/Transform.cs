using System;
using System.Collections;

namespace Xamarin.Robotics.SpecializedBlocks
{
    public delegate double TransformFunction (double input);

    public class Transform : BlockBase
    {
        public Port Input { get; private set; }
        public Port Output { get; private set; }

        public TransformFunction Function { get; set; }

        public Transform ()
        {
            Input = AddPort ("Input", Units.Scalar);
            Output = AddPort ("Output", Units.Scalar);

            Input.ValueChanged += (s,e) => Run ();
        }

        void Run ()
        {
            if (Function == null) {
                Output.Value = Input.Value;
            }
            else {
                Output.Value = Function (Input.Value);
            }
        }
    }
}
