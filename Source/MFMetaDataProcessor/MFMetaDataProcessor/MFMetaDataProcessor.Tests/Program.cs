using System.IO;
using Mono.Cecil;

namespace MFMetaDataProcessor.Tests
{
    class Program
    {
        static void Main()
        {
            var assemblyDefinition =  AssemblyDefinition.ReadAssembly(@"Data\NetduinoOne\NetduinoOne.exe");

            using (var stream = File.Open(@"Data\NetduinoOne\le\NetduinoOne.pex", FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new BinaryWriter(stream))
            {
                var leBuilder = new TinyAssemblyBuilder(assemblyDefinition);
                leBuilder.Write(TinyBinaryWriter.CreateLittleEndianBinaryWriter(writer));
            }

            using (var stream = File.Open(@"Data\NetduinoOne\be\NetduinoOne.pex", FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new BinaryWriter(stream))
            {
                var leBuilder = new TinyAssemblyBuilder(assemblyDefinition);
                leBuilder.Write(TinyBinaryWriter.CreateBigEndianBinaryWriter(writer));
            }
        }
    }
}
