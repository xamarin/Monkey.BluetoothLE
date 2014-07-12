using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    internal sealed class TinyPdbxFileWriter
    {
        private readonly TinyTablesContext _context;

        public TinyPdbxFileWriter(
            TinyTablesContext context)
        {
            _context = context;
        }

        public void Write(
            XmlWriter writer)
        {
            writer.WriteStartElement("PdbxFile");
            writer.WriteStartElement("Assembly");

            WriteTokensPair(writer, _context.AssemblyDefinition.MetadataToken.ToUInt32(), 0x00000000);
            writer.WriteElementString("FileName", _context.AssemblyDefinition.MainModule.Name);
            WriteVersionInfo(writer, _context.AssemblyDefinition.Name.Version);

            writer.WriteStartElement("Classes");
            _context.TypeDefinitionTable.ForEachItems((token, item) => WriteClassInfo(writer, token, item));

            writer.WriteEndDocument();            
        }

        private void WriteVersionInfo(
            XmlWriter writer,
            Version version)
        {
            writer.WriteStartElement("Version");

            writer.WriteElementString("Major", version.Major.ToString("D", CultureInfo.InvariantCulture));
            writer.WriteElementString("Minor", version.Minor.ToString("D", CultureInfo.InvariantCulture));
            writer.WriteElementString("Build", version.Build.ToString("D", CultureInfo.InvariantCulture));
            writer.WriteElementString("Revision", version.Revision.ToString("D", CultureInfo.InvariantCulture));

            writer.WriteEndElement();
        }

        private void WriteClassInfo(
            XmlWriter writer,
            UInt32 tinyClrItemToken,
            TypeDefinition item)
        {
            writer.WriteStartElement("Class");

            WriteTokensPair(writer, item.MetadataToken.ToUInt32(), 0x04000000 | tinyClrItemToken);

            writer.WriteStartElement("Methods");
            foreach (var tuple in GetMethodsTokens(item.Methods))
            {
                writer.WriteStartElement("Method");

                WriteTokensPair(writer, tuple.Item1, tuple.Item2);

                if (!tuple.Item3.HasBody)
                {
                    writer.WriteElementString("HasByteCode", "false");
                }
                writer.WriteStartElement("ILMap");
                foreach (var offset in _context.TypeDefinitionTable.GetByteCodeOffsets(tuple.Item1))
                {
                    writer.WriteStartElement("IL");

                    writer.WriteElementString("CLR", "0x" + offset.Item1.ToString("X8", CultureInfo.InvariantCulture));
                    writer.WriteElementString("TinyCLR", "0x" + offset.Item2.ToString("X8", CultureInfo.InvariantCulture));

                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("Fields");
            foreach (var pair in GetFieldsTokens(item.Fields))
            {
                writer.WriteStartElement("Field");

                WriteTokensPair(writer, pair.Item1, pair.Item2);

                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        private IEnumerable<Tuple<UInt32, UInt32, MethodDefinition>> GetMethodsTokens(
            IEnumerable<MethodDefinition> methods)
        {
            foreach (var method in methods)
            {
                UInt16 fieldToken;
                _context.MethodDefinitionTable.TryGetMethodReferenceId(method, out fieldToken);
                yield return new Tuple<UInt32, UInt32, MethodDefinition>(
                    method.MetadataToken.ToUInt32(), 0x06000000 | (UInt32)fieldToken, method);
            }
        }

        private IEnumerable<Tuple<UInt32, UInt32>> GetFieldsTokens(
            IEnumerable<FieldDefinition> fields)
        {
            foreach (var field in fields.Where(item => !item.HasConstant))
            {
                UInt16 fieldToken;
                _context.FieldsTable.TryGetFieldReferenceId(field, false, out fieldToken);
                yield return new Tuple<UInt32, UInt32>(
                    field.MetadataToken.ToUInt32(), 0x05000000 | (UInt32)fieldToken);
            }
        }

        private void WriteTokensPair(
            XmlWriter writer,
            UInt32 clrToken,
            UInt32 tinyClrToken)
        {
            writer.WriteStartElement("Token");

            writer.WriteElementString("CLR", "0x" + clrToken.ToString("X8", CultureInfo.InvariantCulture));
            writer.WriteElementString("TinyCLR", "0x" + tinyClrToken.ToString("X8", CultureInfo.InvariantCulture));

            writer.WriteEndElement();
        }
    }
}
