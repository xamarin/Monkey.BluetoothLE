using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Mono.Cecil;

namespace Microsoft.SPOT.Debugger
{
	public class CorDebugClass
	{
		CorDebugAssembly m_assembly;
		Pdbx.Class m_pdbxClass;
		uint m_tkSymbolless;

		public CorDebugClass (CorDebugAssembly assembly, Pdbx.Class cls)
		{
			m_assembly = assembly;
			m_pdbxClass = cls;
		}

		public CorDebugClass (CorDebugAssembly assembly, uint tkSymbolless) : this (assembly, null)
		{
			m_tkSymbolless = tkSymbolless;
		}

		public CorDebugAssembly Assembly {
			[System.Diagnostics.DebuggerHidden]
            get { return m_assembly; }
		}

		public bool IsEnum {
			get {
				if (HasSymbols && Assembly.MetaData != null)
					return (Assembly.MetaData.LookupToken ((int)m_pdbxClass.Token.CLR)as TypeDefinition).IsEnum;
				else
					return false;
			}
		}

		public Engine Engine {
			[System.Diagnostics.DebuggerHidden]
            get { return this.Process.Engine; }
		}

		public CorDebugProcess Process {
			[System.Diagnostics.DebuggerHidden]
            get { return this.Assembly.Process; }
		}

		public CorDebugAppDomain AppDomain {
			[System.Diagnostics.DebuggerHidden]
            get { return this.Assembly.AppDomain; }
		}

		public Pdbx.Class PdbxClass {
			[System.Diagnostics.DebuggerHidden]
            get { return m_pdbxClass; }
		}

		public bool HasSymbols {
			get { return m_pdbxClass != null; }
		}

		public uint TypeDef_Index {
			get {
				uint tk = HasSymbols ? m_pdbxClass.Token.TinyCLR : m_tkSymbolless;

				return TinyCLR_TypeSystem.ClassMemberIndexFromTinyCLRToken (tk, this.Assembly);                
			}
		}
	}
}
