using Mono.Debugging.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoDevelop.MicroFramework
{
	class MicroFrameworkDebuggerStartInfo : DebuggerStartInfo
	{
		public MicroFrameworkExecutionCommand MFCommand { get; private set; }

		public MicroFrameworkDebuggerStartInfo(MicroFrameworkExecutionCommand command)
		{
			MFCommand = command;
		}
	}
}
