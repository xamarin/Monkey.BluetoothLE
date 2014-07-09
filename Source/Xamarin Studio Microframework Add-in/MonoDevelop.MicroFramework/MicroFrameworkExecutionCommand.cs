using Microsoft.SPOT.Debugger;
using MonoDevelop.Core.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoDevelop.MicroFramework
{
	class MicroFrameworkExecutionCommand : ProcessExecutionCommand
	{
		public MicroFrameworkExecutionCommand()
		{
		}


//		private PortDefinition portDefinition;
//
//		public PortDefinition PortDefinition
//		{
//			get
//			{
//				return portDefinition;
//			}
//			set
//			{
//				if(portDefinition != value)
//				{
//					portDefinition = value;
//					Target = new MicroFrameworkExecutionTarget(value);
//				}
//			}
//		}

		public Core.FilePath OutputDirectory { get; set; }
	}
}
