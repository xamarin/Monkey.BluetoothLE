using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Microsoft.SPOT.Debugger
{
    public class ConnectionPoint
    {
        public class Connections : IEnumerable
        {
            private uint m_dwCookieNext;
            private Hashtable m_ht;

            public Connections()
            {
                m_dwCookieNext = 0;
                m_ht = new Hashtable();
            }

            public void Advise(object pUnkSink, out uint pdwCookie)
            {
                pdwCookie = m_dwCookieNext++;            
                m_ht[pdwCookie] = pUnkSink;
            }

            public void Unadvise(uint dwCookie)
            {
                m_ht.Remove(dwCookie);
            }

            #region IEnumerable Members

            public IEnumerator GetEnumerator()
            {
                ArrayList al = new ArrayList ();

                foreach (object o in m_ht.Values)
                {
                    al.Add (o);
                }

                return al.GetEnumerator ();
            }
            
            #endregion
        }

        public readonly Connections m_connections;
        
        public ConnectionPoint()
        {
            m_connections = new Connections();
        }

        public Connections Sinks
        {
            get {return m_connections;}
        }
    }
}
