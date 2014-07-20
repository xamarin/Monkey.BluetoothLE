using System;
using System.Runtime.InteropServices;

namespace Microsoft.SPOT.Debugger
{
	public class CorDebugCode
	{
		CorDebugFunction m_function;

		public CorDebugCode (CorDebugFunction function)
		{
			m_function = function;
		}

		public CorDebugFunctionBreakpoint CreateBreakpoint (uint offset)
		{
			return new CorDebugFunctionBreakpoint (m_function, offset);
		}
	}
}
