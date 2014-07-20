using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using Mono.Cecil;
using MonoDevelop.MicroFramework;
using Mono.Cecil.Cil;

namespace Microsoft.SPOT.Debugger
{
	public class CorDebugFunction
	{
		CorDebugClass m_class;
		Pdbx.Method m_pdbxMethod;
		CorDebugCode m_codeNative;
		CorDebugCode m_codeIL;
		uint m_tkSymbolless;

		public CorDebugFunction (CorDebugClass cls, Pdbx.Method method)
		{
			m_class = cls;
			m_pdbxMethod = method;            
		}

		public CorDebugFunction (CorDebugClass cls, uint tkSymbolless) : this (cls, null)
		{
			m_tkSymbolless = tkSymbolless;
		}

		public CorDebugClass Class {
			[System.Diagnostics.DebuggerHidden]
            get { return m_class; }
		}

		public CorDebugAppDomain AppDomain {                       
			[System.Diagnostics.DebuggerHidden]
            get { return this.Class.AppDomain; }
		}

		public CorDebugProcess Process {                       
			[System.Diagnostics.DebuggerHidden]
            get { return this.Class.Process; }
		}

		public CorDebugAssembly Assembly {                       
			[System.Diagnostics.DebuggerHidden]
            get { return this.Class.Assembly; }
		}

		private Engine Engine {
			[System.Diagnostics.DebuggerHidden]
            get { return this.Class.Engine; }
		}

		[System.Diagnostics.DebuggerStepThrough]
		private CorDebugCode GetCode (ref CorDebugCode code)
		{
			if (code == null)
				code = new CorDebugCode (this);
			return code;
		}

		public bool HasSymbols {
			get { return m_pdbxMethod != null; }
		}

		public uint MethodDef_Index {
			get {
				uint tk = HasSymbols ? m_pdbxMethod.Token.TinyCLR : m_tkSymbolless;

				return TinyCLR_TypeSystem.ClassMemberIndexFromTinyCLRToken (tk, this.m_class.Assembly);
			}
		}

		public Pdbx.Method PdbxMethod {
			[System.Diagnostics.DebuggerHidden]
            get { return m_pdbxMethod; }
		}

		public bool IsInternal {
			get { return (Class.Assembly.MetaData.LookupToken ((int)this.m_pdbxMethod.Token.CLR) as MethodDefinition).IsInternalCall; }
		}

		public bool IsInstance {
			get { return !(Class.Assembly.MetaData.LookupToken ((int)this.m_pdbxMethod.Token.CLR) as MethodDefinition).IsStatic; }
		}

		public bool IsVirtual {
			get { return (Class.Assembly.MetaData.LookupToken ((int)this.m_pdbxMethod.Token.CLR) as MethodDefinition).IsVirtual; }
		}

		public uint GetILCLRFromILTinyCLR (uint ilTinyCLR)
		{
			uint ilCLR;
            
			//Special case for CatchHandlerFound and AppDomain transitions; possibly used elsewhere.
			if (ilTinyCLR == uint.MaxValue)
				return uint.MaxValue;

			ilCLR = ILComparer.Map (false, m_pdbxMethod.ILMap, ilTinyCLR);
			Debug.Assert (ilTinyCLR <= ilCLR);

			return ilCLR;
		}

		public uint GetILTinyCLRFromILCLR (uint ilCLR)
		{
			//Special case for when CPDE wants to step to the end of the function?
			if (ilCLR == uint.MaxValue)
				return uint.MaxValue;

			uint ilTinyCLR = ILComparer.Map (true, m_pdbxMethod.ILMap, ilCLR);

			Debug.Assert (ilTinyCLR <= ilCLR);

			return ilTinyCLR;
		}

		private class ILComparer : IComparer
		{
			bool m_fCLR;

			private ILComparer (bool fCLR)
			{
				m_fCLR = fCLR;
			}

			private static uint GetIL (bool fCLR, Pdbx.IL il)
			{
				return fCLR ? il.CLR : il.TinyCLR;
			}

			private uint GetIL (Pdbx.IL il)
			{
				return GetIL (m_fCLR, il);
			}

			private static void SetIL (bool fCLR, Pdbx.IL il, uint offset)
			{
				if (fCLR)
					il.CLR = offset;
				else
					il.TinyCLR = offset;
			}

			private void SetIL (Pdbx.IL il, uint offset)
			{
				SetIL (m_fCLR, il, offset);
			}

			public int Compare (object o1, object o2)
			{
				return GetIL (o1 as Pdbx.IL).CompareTo (GetIL (o2 as Pdbx.IL));
			}

			public static uint Map (bool fCLR, Pdbx.IL[] ilMap, uint offset)
			{
				ILComparer ilComparer = new ILComparer (fCLR);
				Pdbx.IL il = new Pdbx.IL ();
				ilComparer.SetIL (il, offset);
				int i = Array.BinarySearch (ilMap, il, ilComparer);
				uint ret = 0;

				if (i >= 0) {
					//Exact match
					ret = GetIL (!fCLR, ilMap [i]);
				} else {

					i = ~i;

					if (i == 0) {
						//Before the IL diverges
						ret = offset;
					} else {
						//Somewhere in between
						i--;

						il = ilMap [i];
						ret = offset - GetIL (fCLR, il) + GetIL (!fCLR, il);
					}
				}

				Debug.Assert (ret >= 0);
				return ret;
			}
		}

		public uint Token {
			get {
				return HasSymbols ? m_pdbxMethod.Token.CLR : m_tkSymbolless;
			}
		}

		public CorDebugCode ILCode {
			get {
				return GetCode (ref m_codeIL);
			}
		}

		public MethodDefinition GetMethodInfo (MicroFrameworkDebuggerSession session)
		{
			return Assembly.MetaData != null ? Assembly.MetaData.LookupToken ((int)Token) as MethodDefinition : null;
		}

		public MethodSymbols GetMethodSymbols (MicroFrameworkDebuggerSession session)
		{
			if (Assembly.DebugData == null)
				return null;
			var methodSymols = new MethodSymbols (new MetadataToken (PdbxMethod.Token.CLR));
			//Ugliest hack ever
			if(Assembly.DebugData is Mono.Cecil.Mdb.MdbReader) {
				for(int i = 0; i < 100; i++)
					methodSymols.Variables.Add(new VariableDefinition(null));
			}
			Assembly.DebugData.Read (methodSymols);
			return methodSymols;
		}
	}
}
