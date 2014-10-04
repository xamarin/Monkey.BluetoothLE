using System;
using Microsoft.SPOT;

namespace Robotics.Micro.SpecializedBlocks
{
    public class LookupTable : Block, System.Collections.IEnumerable
    {
        public InputPort Input { get; private set; }
        public OutputPort Output { get; private set; }

        const int MaxEntries = 64;
        readonly LookupTableEntry[] entries = new LookupTableEntry[MaxEntries];
        int numEntries = 0;

        public LookupTable ()
        {
            Input = AddInput ("Input", Units.Scalar);
            Output = AddOutput ("Output", Units.Scalar, 0);

            Input.ValueChanged += (s, e) => SetOutput ();

            SetOutput ();
        }

        public void Add (double x, double y)
        {
            if (numEntries + 1 >= MaxEntries)
                throw new InvalidOperationException ("Too many points. Max = " + MaxEntries);
            if (numEntries == 0) {
                numEntries = 1;
                entries[0].X = x;
                entries[0].Y = y;
                return;
            }
            if (x <= entries[numEntries - 1].X)
                throw new InvalidOperationException ("Entries must be added in increasing X order");

            entries[numEntries].X = x;
            entries[numEntries].Y = y;
            numEntries++;
        }

        void SetOutput ()
        {
            Output.Value = Interpolate (Input.Value);
        }

        double Interpolate (double x)
        {
            if (numEntries <= 0)
                return 0;

            // Find the index with e.X > x
            var next = -1;
            for (var i = 0; i < numEntries; i++) {
                if (x < entries[i].X) {
                    next = i;
                    break;
                }
            }

            // We're before the beginning, just keep a constant
            if (next == 0) {
                return entries[0].Y;
            }

            // We're after the end, just keep a constant
            if (next < 0) {
                return entries[numEntries - 1].Y;
            }

            var px = entries[next - 1].X;
            var py = entries[next - 1].Y;

            var dx = entries[next].X - px;
            var dy = entries[next].Y - py;

            var u = (x - px) / dx;

            return py + u * dy;
        }

        struct LookupTableEntry
        {
            public double X;
            public double Y;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            for (var i = 0; i < numEntries; i++) {
                yield return entries[i].X; // Probably not too useful
            }
        }
    }
}
