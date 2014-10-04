using System;
using Microsoft.SPOT;

namespace Robotics.Micro.SpecializedBlocks
{
    /// <summary>
    /// This one is a little strange. It adds and removes
    /// connections based upon in integral number.
    /// </summary>
    public class Switch : Block
    {
        public InputPort Input { get; private set; }

        readonly Connection[][] groups;

        int connectedIndex = -1;
        Connection[] connections = null;

        public Switch (params Connection[][] connectionGroups)
        {
            groups = connectionGroups;

            Input = AddInput ("Input", Units.Scalar);

            Input.ValueChanged += (s, e) => SetSelection ();

            SetSelection ();
        }

        void SetSelection ()
        {
            var index = System.Math.Min (System.Math.Max ((int)(Input.Value + 0.5), 0), groups.Length - 1);
            if (index != connectedIndex) {
                ChangeConnections (index);
            }
        }

        void ChangeConnections (int newIndex)
        {
            // Debug.Print ("Switching to " + newIndex);

            if (connections != null) {
                foreach (var c in connections) {
                    c.Disconnect ();
                }
            }

            connections = groups[newIndex];
            connectedIndex = newIndex;

            if (connections != null) {
                foreach (var c in connections) {
                    c.Connect ();
                }
            }
        }
    }
}
