using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.SPOT.Debugger
{
	public class CorDebugAssembly:IDisposable
	{
		CorDebugAppDomain m_appDomain;
		CorDebugProcess m_process;
		Hashtable m_htTokenCLRToPdbx;
		Hashtable m_htTokenTinyCLRToPdbx;
		Pdbx.PdbxFile m_pdbxFile;
		Pdbx.Assembly m_pdbxAssembly;
		uint m_idx;
		string m_name;
		string m_path;
		CorDebugAssembly m_primaryAssembly;
		bool m_isFrameworkAssembly;

		public CorDebugAssembly (CorDebugProcess process, string name, Pdbx.PdbxFile pdbxFile, uint idx)
		{
			m_process = process;
			m_appDomain = null;
			m_name = name;
			m_pdbxFile = pdbxFile;
			m_pdbxAssembly = (pdbxFile != null) ? pdbxFile.Assembly : null;
			m_htTokenCLRToPdbx = new Hashtable ();
			m_htTokenTinyCLRToPdbx = new Hashtable ();
			m_idx = idx;
			m_primaryAssembly = null;
			m_isFrameworkAssembly = false;

			if (m_pdbxAssembly != null) {
				if (!string.IsNullOrEmpty (pdbxFile.PdbxPath)) {
					string pth = pdbxFile.PdbxPath.ToLower ();

					if (pth.Contains (@"\buildoutput\")) {
#region V4_1_FRAMEWORK_ASSEMBLIES
						List<string> frameworkAssemblies = new List<string> {
							"mfdpwsclient",
							"mfdpwsdevice",
							"mfdpwsextensions",
							"mfwsstack",
							"microsoft.spot.graphics",
							"microsoft.spot.hardware",
							"microsoft.spot.hardware.serialport",
							"microsoft.spot.hardware.usb",
							"microsoft.spot.ink",
							"microsoft.spot.io",
							"microsoft.spot.native",
							"microsoft.spot.net",
							"microsoft.spot.net.security",
							"microsoft.spot.time",
							"microsoft.spot.tinycore",
							"microsoft.spot.touch",
							"mscorlib",
							"system.http",
							"system.io",
							"system.net.security",
							"system",
							"system.xml.legacy",
							"system.xml", 
						};
#endregion // V4_1_FRAMEWORK_ASSEMBLIES

						m_isFrameworkAssembly = (frameworkAssemblies.Contains (name.ToLower ()));
					} else {
						m_isFrameworkAssembly = pdbxFile.PdbxPath.ToLower().Contains(Path.PathSeparator + ".net micro framework" + Path.PathSeparator);
					}
				}

				m_pdbxAssembly.CorDebugAssembly = this;
				foreach (Pdbx.Class c in m_pdbxAssembly.Classes) {
					AddTokenToHashtables (c.Token, c);
					foreach (Pdbx.Field field in c.Fields) {
						AddTokenToHashtables (field.Token, field);
					}

					foreach (Pdbx.Method method in c.Methods) {
						AddTokenToHashtables (method.Token, method);
					}
				}

				if (File.Exists (Path.ChangeExtension (m_pdbxFile.PdbxPath, ".dll")))
					MetaData = ModuleDefinition.ReadModule (Path.ChangeExtension (m_pdbxFile.PdbxPath, ".dll"));
				else if (File.Exists (Path.ChangeExtension (m_pdbxFile.PdbxPath, ".exe")))
					MetaData = ModuleDefinition.ReadModule (Path.ChangeExtension (m_pdbxFile.PdbxPath, ".exe"));

				if (MetaData != null && File.Exists (Path.ChangeExtension (m_pdbxFile.PdbxPath, ".pdb"))) {
					DebugData = new Mono.Cecil.Pdb.PdbReaderProvider ().GetSymbolReader (MetaData, Path.ChangeExtension (m_pdbxFile.PdbxPath, ".pdb"));
					MetaData.ReadSymbols (DebugData);
				} else if (MetaData != null && File.Exists (Path.ChangeExtension (m_pdbxFile.PdbxPath, ".exe.mdb"))) {
					DebugData = new Mono.Cecil.Mdb.MdbReaderProvider ().GetSymbolReader (MetaData, Path.ChangeExtension (m_pdbxFile.PdbxPath, ".exe"));
					MetaData.ReadSymbols (DebugData);
				} else if (MetaData != null && File.Exists (Path.ChangeExtension (m_pdbxFile.PdbxPath, ".dll.mdb"))) {
					DebugData = new Mono.Cecil.Mdb.MdbReaderProvider ().GetSymbolReader (MetaData, Path.ChangeExtension (m_pdbxFile.PdbxPath, ".dll"));
					MetaData.ReadSymbols (DebugData);
				}
			}
		}

		private bool IsPrimaryAssembly {
			get { return m_primaryAssembly == null; }
		}

		public bool IsFrameworkAssembly {
			get { return m_isFrameworkAssembly; }
		}

		public CorDebugAssembly CreateAssemblyInstance (CorDebugAppDomain appDomain)
		{
			CorDebugAssembly assm = (CorDebugAssembly)MemberwiseClone ();
			assm.m_appDomain = appDomain;
			assm.m_primaryAssembly = this;

			return assm;
		}

		public static CorDebugAssembly AssemblyFromIdx (uint idx, ArrayList assemblies)
		{
			foreach (CorDebugAssembly assembly in assemblies) {
				if (assembly.Idx == idx)
					return assembly;
			}
			return null;
		}

		public static CorDebugAssembly AssemblyFromIndex (uint index, ArrayList assemblies)
		{
			return AssemblyFromIdx (TinyCLR_TypeSystem.IdxAssemblyFromIndex (index), assemblies);
		}

		public string Name {
			get { return m_name; }
		}

		public bool HasSymbols {
			get { return m_pdbxAssembly != null; }
		}

		private void AddTokenToHashtables (Pdbx.Token token, object o)
		{
			m_htTokenCLRToPdbx [token.CLR] = o;
			m_htTokenTinyCLRToPdbx [token.TinyCLR] = o;
		}

		private string FindAssemblyOnDisk ()
		{
			if (m_path == null && m_pdbxAssembly != null) {
				string[] pathsToTry = new string[] {
					// Look next to pdbx file
					Path.Combine (Path.GetDirectoryName (m_pdbxFile.PdbxPath), m_pdbxAssembly.FileName),
					// look next to the dll for the SDK (C:\Program Files\Microsoft .NET Micro Framework\<version>\Assemblies\le|be)
					Path.Combine (Path.GetDirectoryName (m_pdbxFile.PdbxPath), @"..\" + m_pdbxAssembly.FileName),
					// look next to the dll for the PK (SPOCLIENT\buildoutput\public\<buildtype>\client\pe\le|be)
					Path.Combine (Path.GetDirectoryName (m_pdbxFile.PdbxPath), @"..\..\dll\" + m_pdbxAssembly.FileName),
				};

				for (int iPath = 0; iPath < pathsToTry.Length; iPath++) {
					string path = pathsToTry [iPath];

					if (File.Exists (path)) {
						//is this the right file?
						m_path = path;
						break;
					}
				}
			}

			return m_path;
		}

		public ModuleDefinition MetaData {
			get;
			set;
		}

		public CorDebugProcess Process {
			[System.Diagnostics.DebuggerHidden]
            get { return m_process; }
		}

		public CorDebugAppDomain AppDomain {
			[System.Diagnostics.DebuggerHidden]
            get { return m_appDomain; }
		}

		public uint Idx {
			[System.Diagnostics.DebuggerHidden]
            get { return m_idx; }
		}

		private CorDebugFunction GetFunctionFromToken (uint tk, Hashtable ht)
		{
			CorDebugFunction function = null;
			Pdbx.Method method = ht [tk] as Pdbx.Method;
			if (method != null) {
				CorDebugClass c = new CorDebugClass (this, method.Class);
				function = new CorDebugFunction (c, method);
			}

			Debug.Assert (function != null);
			return function;
		}

		public CorDebugFunction GetFunctionFromTokenCLR (uint tk)
		{
			return GetFunctionFromToken (tk, m_htTokenCLRToPdbx);
		}

		public CorDebugFunction GetFunctionFromTokenTinyCLR (uint tk)
		{
			if (HasSymbols) {
				return GetFunctionFromToken (tk, m_htTokenTinyCLRToPdbx);
			} else {
				uint index = TinyCLR_TypeSystem.ClassMemberIndexFromTinyCLRToken (tk, this);

				WireProtocol.Commands.Debugging_Resolve_Method.Result resolvedMethod = this.Process.Engine.ResolveMethod (index);
				Debug.Assert (TinyCLR_TypeSystem.IdxAssemblyFromIndex (resolvedMethod.m_td) == this.Idx);

				uint tkMethod = TinyCLR_TypeSystem.SymbollessSupport.MethodDefTokenFromTinyCLRToken (tk);
				uint tkClass = TinyCLR_TypeSystem.TinyCLRTokenFromTypeIndex (resolvedMethod.m_td);

				CorDebugClass c = GetClassFromTokenTinyCLR (tkClass);

				return new CorDebugFunction (c, tkMethod);
			}
		}

		public Pdbx.ClassMember GetPdbxClassMemberFromTokenCLR (uint tk)
		{
			return m_htTokenCLRToPdbx [tk] as Pdbx.ClassMember;
		}

		private CorDebugClass GetClassFromToken (uint tk, Hashtable ht)
		{
			CorDebugClass cls = null;
			Pdbx.Class c = ht [tk] as Pdbx.Class;
			if (c != null) {
				cls = new CorDebugClass (this, c);
			}

			return cls;
		}

		public CorDebugClass GetClassFromTokenCLR (uint tk)
		{
			return GetClassFromToken (tk, m_htTokenCLRToPdbx);
		}

		public CorDebugClass GetClassFromTokenTinyCLR (uint tk)
		{
			if (HasSymbols)
				return GetClassFromToken (tk, m_htTokenTinyCLRToPdbx);
			else
				return new CorDebugClass (this, TinyCLR_TypeSystem.SymbollessSupport.TypeDefTokenFromTinyCLRToken (tk));
		}

		public void SetJmcStatus (bool fJMC)
		{
			if (this.HasSymbols) {
				if (this.Process.Engine.Info_SetJMC (fJMC, ReflectionDefinition.Kind.REFLECTION_ASSEMBLY, TinyCLR_TypeSystem.IndexFromIdxAssemblyIdx (this.Idx))) {
					if (!this.m_isFrameworkAssembly) {
						//now update the debugger JMC state...
						foreach (Pdbx.Class c in this.m_pdbxAssembly.Classes) {
							foreach (Pdbx.Method m in c.Methods) {
								m.IsJMC = fJMC;
							}
						}
					}
				}
			}
		}

		public ISymbolReader DebugData {
			get;
			set;
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			if (DebugData != null) {
				DebugData.Dispose ();
				DebugData = null;
			}
		}

		#endregion
	}
}
