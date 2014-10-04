using System;
using Microsoft.SPOT;

namespace Robotics.Micro
{
    /// <summary>
    /// This is a reference to a connection. Creation of this object does not make the connection.
    /// You need to call Connect to do that.
    /// </summary>
    public class Connection
    {
        public Port From { get; private set; }
        public Port To { get; private set; }

        public Connection (Port fromPort, Port toPort)
        {
            From = fromPort;
            To = toPort;
        }

        /// <summary>
        /// Connect From and To
        /// </summary>
        public void Connect ()
        {
            From.ConnectTo (To);
        }

        /// <summary>
        /// Disconnect From and To
        /// </summary>
        public void Disconnect ()
        {
            From.DisconnectFrom (To);
        }
    }
}
