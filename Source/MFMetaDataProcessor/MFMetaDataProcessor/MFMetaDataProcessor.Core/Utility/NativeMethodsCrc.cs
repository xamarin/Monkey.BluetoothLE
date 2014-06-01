using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Helper class for calculating native methods CRC value. Really caclulates CRC32 value
    /// for native method signatures (not methods itself) and signatures treated as string
    /// values, formatted by weird rules incompartible with all rest codebase.
    /// </summary>
    public sealed class NativeMethodsCrc
    {
        private readonly Byte[] _null = Encoding.ASCII.GetBytes("NULL");

        private readonly Byte[] _name;

        public NativeMethodsCrc(
            AssemblyDefinition assembly)
        {
            _name = Encoding.ASCII.GetBytes(assembly.Name.Name);
        }

        public UInt32 Current { get; private set; }

        public void UpdateCrc(MethodDefinition method)
        {
            var type = method.DeclaringType;
            if ((type.IsClass || type.IsValueType) &&
                (method.RVA == 0xFFFFFFF && !method.IsAbstract))
            {
                Current = Crc32.Compute(_name, Current);
                Current = Crc32.Compute(Encoding.ASCII.GetBytes(GetClassName(type)), Current);
                Current = Crc32.Compute(Encoding.ASCII.GetBytes(GetMethodName(method)), Current);
            }
            else
            {
                Current = Crc32.Compute(_null, Current);
            }
        }

        private String GetClassName(
            TypeDefinition type)
        {
            return (type != null
                ? String.Concat(GetClassName(type.DeclaringType), type.Namespace, type.Name)
                    .Replace(".", String.Empty)
                : String.Empty);
        }

        private String GetMethodName(
            MethodDefinition method)
        {
            var name = String.Concat(method.Name, (method.IsStatic ? "___STATIC__" : "___"),
                String.Join("__", GetAllParameters(method)));

            // TODO: add number suffix for non-unique method names
            return name.Replace(".", String.Empty);
        }

        private IEnumerable<String> GetAllParameters(
            MethodDefinition method)
        {
            yield return GetParameterType(method.ReturnType);

            if (method.HasParameters)
            {
                foreach (var item in method.Parameters)
                {
                    yield return GetParameterType(item.ParameterType);
                }
            }
        }

        private String GetParameterType(
            TypeReference parameterType)
        {
            return parameterType.Name.ToUpper();
        }
    }
}
