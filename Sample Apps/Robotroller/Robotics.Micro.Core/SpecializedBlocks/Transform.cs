using System;
using System.Collections;

namespace Robotics.Micro.SpecializedBlocks
{
    public delegate double TransformFunction (double input);

    public class Transform : Block
    {
        public InputPort Input { get; private set; }
        public OutputPort Output { get; private set; }

        public TransformFunction Function { get; set; }

        public Transform (TransformFunction function = null)
        {
            Function = function;

            Input = AddInput ("Input", Units.Scalar);
            Output = AddOutput ("Output", Units.Scalar);

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
