using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robotics.Micro.Core.WindowsIoTTests
{
    public abstract class Test
    {
        public abstract string Title { get; }
        public abstract void Run ();
    }
}
