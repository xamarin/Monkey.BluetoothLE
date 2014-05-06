using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    public class Parser
    {
		AssemblyImport AssemblyImport;
		AssemblyDefinition scopeAssembly;

		string assemblyName;
		Version version;

		bool noByteCode;
		List<MethodDefinition> mapDef_Method;

		public List<CustomAttribute> mapDef_CustomAttribute;

		/// <summary>
		/// Matches
		/// HRESULT MetaData::Parser::Analyze( LPCWSTR szFileName )
		/// from AssemblyParser.cpp:2303 in CLR/Tools/Parser
		/// </summary>
		public void Analyze (string fileName)
        {
            GetAssemblyDef ();

			EnumAssemblyRefs ();
			EnumModuleRefs ();
			EnumTypeRefs ();

			EnumTypeDefs ();
			EnumTypeSpecs ();
			EnumUserStrings ();

			// TODO: Record the SPOT attributes

			if (!noByteCode) {
				foreach (var md in mapDef_Method) {
					if (md.HasBody && md.Body.CodeSize > 0) {
						ByteCode.VerifyConsitency (md);
					}
				}
			}
        }

		void EnumAssemblyRefs ()
		{
			foreach (var asm in AssemblyImport.Assemblies) {
				GetAssemblyRef (asm);
			}
		}

		void EnumModuleRefs ()
		{
			throw new NotImplementedException ();
		}

		void EnumTypeRefs ()
		{
			throw new NotImplementedException ();
		}

		void EnumTypeDefs ()
		{
			throw new NotImplementedException ();
		}

		void EnumTypeSpecs ()
		{
			throw new NotImplementedException ();
		}

		void EnumUserStrings ()
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Matches
		/// HRESULT MetaData::Parser::GetAssemblyDef()
		/// from AssemblyParser.cpp:1326 in CLR/Tools/Parser
		/// </summary>
		void GetAssemblyDef ()
		{
			assemblyName = scopeAssembly.Name.Name;
			version = scopeAssembly.Name.Version;
			// TODO: Finish collecting metadata
		}


		/// <summary>
		/// Matches
		/// HRESULT MetaData::Parser::GetAssemblyRef( mdAssemblyRef ar )
		/// from AssemblyParser.cpp:1369 in CLR/Tools/Parser
		/// </summary>
		void GetAssemblyRef (AssemblyDefinition asm)
		{
			throw new NotImplementedException ();
		}
    }
}
