using System;
using System.Collections.Generic;
using Mono.Cecil;
using System.IO;

namespace MFMetaDataProcessor
{
	public class Linker
	{
		Parser pr;

		/// <summary>
		/// Matches
		/// HRESULT WatchAssemblyBuilder::Linker::Process( MetaData::Parser& pr )
		/// from Linker.cpp:864 in CLR/Tools/Parser
		/// </summary>
		public void Process (Parser pr)
		{
			this.pr = pr;

			foreach (var ca in pr.mapDef_CustomAttribute) {
				// TODO: process attributes
			}

			//
			// First string of heap is always the null string!!
			//
			{
				// TODO: Make sense of this
//				CLR_STRING idx;
//
//				if(!AllocString( std::wstring(), idx, true )) REPORT_NO_MEMORY();
			}

			//--//

			{
				var order = new List<TypeDefinition> ();

				ProcessAssemblyRef ();
				ProcessTypeRef ();
				ProcessMemberRef ();
				ProcessTypeDef (order);
				ProcessTypeSpec ();
				ProcessAttribute ();
				ProcessResource ();
				ProcessUserString ();

				foreach (var td in order) {
					ProcessTypeDef_ByteCode(td);
				}
			}
		}

		/// <summary>
		/// Matches
		/// HRESULT WatchAssemblyBuilder::Linker::Generate( CQuickRecord<BYTE>& buf, bool patch_fReboot, bool patch_fSign, std::wstring* patch_szNative )
		/// from Linker.cpp:2369 in CLR/Tools/Parser
		/// </summary>
		public void Generate (BinaryWriter buf, bool reboot, bool sign, string native)
		{
			// TODO: Apply patches

			ClrRecordAssembly header;

			EmitData (buf, out header);

			// TODO: Sign
		}

		/// <summary>
		/// Matches
		/// HRESULT WatchAssemblyBuilder::Linker::EmitData( CQuickRecord<BYTE>& buf, CLR_RECORD_ASSEMBLY& headerSrc )
		/// from Linker.cpp:2332 in CLR/Tools/Parser
		/// </summary>
		void EmitData (BinaryWriter buf, out ClrRecordAssembly header)
		{
			header = new ClrRecordAssembly ();

			// TODO: Finish emitting data
			throw new NotImplementedException ();
		}

		void ProcessAssemblyRef ()
		{
			throw new NotImplementedException ();
		}

		void ProcessTypeRef ()
		{
			throw new NotImplementedException ();
		}

		void ProcessMemberRef ()
		{
			throw new NotImplementedException ();
		}

		void ProcessTypeDef (List<TypeDefinition> order)
		{
			throw new NotImplementedException ();
		}

		void ProcessTypeSpec ()
		{
			throw new NotImplementedException ();
		}

		void ProcessAttribute ()
		{
			throw new NotImplementedException ();
		}

		void ProcessResource ()
		{
			throw new NotImplementedException ();
		}

		void ProcessUserString ()
		{
			throw new NotImplementedException ();
		}

		void ProcessTypeDef_ByteCode (TypeDefinition td)
		{
			throw new NotImplementedException ();
		}
	}
}

