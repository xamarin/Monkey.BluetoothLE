using Microsoft.SPOT.Debugger;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Debugger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace MonoDevelop.MicroFramework
{
	class MicroFrameworkExecutionHandler : IExecutionHandler
	{
		public bool CanExecute(ExecutionCommand command)
		{
			return command is MicroFrameworkExecutionCommand;
		}

		public IProcessAsyncOperation Execute(ExecutionCommand command, IConsole console)
		{
			return DebuggingService.GetExecutionHandler().Execute(command, console);
		}
	}
}
