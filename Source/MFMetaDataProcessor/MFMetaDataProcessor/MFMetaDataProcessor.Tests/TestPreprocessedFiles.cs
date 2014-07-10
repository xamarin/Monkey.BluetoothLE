using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Xsl;
using Mono.Cecil;
using NUnit.Framework;

namespace MFMetaDataProcessor.Tests
{
    [TestFixture]
    public sealed class TestPreprocessedFiles
    {
        private static readonly List<String> _typesOrder = new List<String>();

        private static readonly XslTransform _pdbxSorter = new XslTransform();

        public TestPreprocessedFiles()
        {
            _pdbxSorter.Load("PdbxSorter.xslt");
        }

        [TearDown]
        public void TestTearDown()
        {
            _typesOrder.Clear();
        }

        [Test]
        [Ignore("Stack size")]
        public void ClockSampleTest()
        {
            TestSingleAssembly("Clock",
                 "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore", "Microsoft.SPOT.Hardware",
                 "Microsoft.SPOT.Net", "Microsoft.SPOT.Time", "Microsoft.SPOT.Graphics");
        }

        [Test]
        public void ExtendedWeakReferencesTest()
        {
            TestSingleAssembly("ExtendedWeakReferences",
                "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore");
        }

        [Test]
        [Ignore("Stack size")]
        public void FileSystemSampleTest()
        {
            _typesOrder.AddRange(new[]
            {
                "FileSystemSample.MyFileSystem",
                "FileSystemSample.Resources",
                "FileSystemSample.Resources/FontResources",
                "FileSystemSample.MyFileSystem/ListView",
                "FileSystemSample.MyFileSystem/ListViewColumn",
                "FileSystemSample.MyFileSystem/ListViewItem",
                "FileSystemSample.MyFileSystem/ListViewSubItem",
                "FileSystemSample.MyFileSystem/MyWindow",
                "FileSystemSample.Resources/StringResources",
                "FileSystemSample.MyFileSystem/ListView/VerticalScrollBar",
            });
            TestSingleAssembly("FileSystemSample",
                "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore", "Microsoft.SPOT.IO",
                "Microsoft.SPOT.Graphics");
        }

        [Test]
        [Ignore("Stack size and string order")]
        public void FtpServerSampleTest()
        {
            TestSingleAssembly("FtpServer",
                "System.Ftp", "Microsoft.SPOT.IO");
        }

        [Test]
        [Ignore("Unused field indexes")]
        public void HelloWorldClientSampleTest()
        {
            _typesOrder.AddRange(new[]
            {
                "Microsoft.SPOT.Sample.MFSimpleServiceClient",
                "Microsoft.SPOT.Sample.TestApplication",
                "localhost.ServiceHelloWCF.HelloWCF",
                "localhost.ServiceHelloWCF.HelloWCFDataContractSerializer",
                "localhost.ServiceHelloWCF.HelloWCFResponse",
                "localhost.ServiceHelloWCF.HelloWCFResponseDataContractSerializer",
                "localhost.ServiceHelloWCF.IServiceHelloWCF",
                "localhost.ServiceHelloWCF.ServiceHelloWCF",
                "localhost.ServiceHelloWCF.ServiceHelloWCFClientProxy"
            });
            TestSingleAssembly("HelloWorldClient",
                "MFWsStack", "Microsoft.SPOT.Net", "System.Http");
        }

        [Test]
        public void HelloWcfServerSampleTest()
        {
            _typesOrder.AddRange(new[]
            {
                "Dpws.Device.Program",
                "localhost.ServiceHelloWCF.HelloWCF",
                "localhost.ServiceHelloWCF.HelloWCFDataContractSerializer",
                "localhost.ServiceHelloWCF.HelloWCFResponse",
                "localhost.ServiceHelloWCF.HelloWCFResponseDataContractSerializer",
                "localhost.ServiceHelloWCF.HelloWCFService",
                "localhost.ServiceHelloWCF.IIServiceHelloWCF",
                "localhost.ServiceHelloWCF.IServiceHelloWCF",
                "localhost.ServiceHelloWCF.ServiceHelloWCFImplementation"
            });
            TestSingleAssembly("HelloWCFServer",
                "Microsoft.SPOT.Net", "MFWsStack");
        }

        [Test]
        public void HttpClientSampleTest()
        {
            TestSingleAssembly("HTTPClient",
                "System.Http", "Microsoft.SPOT.Native");
        }

        [Test]
        public void HttpServerSampleTest()
        {
            _typesOrder.AddRange(new[]
            {
                "HttpServerSample.Resource1",
                "HttpServerSample.Resource1/BinaryResources",
                "HttpServerSample.MyHttpServer",
                "HttpServerSample.MyHttpServer/PrefixKeeper",
                "HttpServerSample.Resource1/StringResources",
            });
            TestSingleAssembly("HTTPServer",
                "System.Http", "Microsoft.SPOT.Native", "Microsoft.SPOT.Update",
                "MFUpdate", "Microsoft.SPOT.IO");
        }

        [Test]
        [Ignore("Stack size")]
        public void InkCanvasSampleTest()
        {
            _typesOrder.AddRange(new[]
            {
                "InkCanvasSample.MyInkCanvas",
                "InkCanvasSample.MyInkCanvas/Button",
                "InkCanvasSample.Resources",
                "InkCanvasSample.Resources/FontResources",
                "InkCanvasSample.MyInkCanvas/MyCanvas",
                "InkCanvasSample.MyInkCanvas/PaletteControl",
                "InkCanvasSample.MyInkCanvas/MyColorPalette",
                "InkCanvasSample.MyInkCanvas/MyWindow",
                "InkCanvasSample.MyInkCanvas/PaletteControlEventHandler",
                "InkCanvasSample.MyInkCanvas/PaletteEventArg",
                "InkCanvasSample.Resources/StringResources"
            });
            TestSingleAssembly("InkCanvasSample",
                "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore", "Microsoft.SPOT.Touch",
                "Microsoft.SPOT.Graphics", "Microsoft.SPOT.Ink", "Microsoft.SPOT.Hardware");
        }

        [Test]
        public void UsbMouseSampleTest()
        {
            _typesOrder.AddRange(new[]
            {
                "<PrivateImplementationDetails>{5B04E18E-C178-4A8D-9F9A-B891A012052A}",
                "USBMouseSample.MyUSBMouse",
                "USBMouseSample.MyUSBMouse/ButtonList",
                "<PrivateImplementationDetails>{5B04E18E-C178-4A8D-9F9A-B891A012052A}/__StaticArrayInitTypeSize=50",
                "<PrivateImplementationDetails>{5B04E18E-C178-4A8D-9F9A-B891A012052A}/__StaticArrayInitTypeSize=7"
            });
            TestSingleAssembly("USBMouse",
                "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore", "Microsoft.SPOT.Hardware.Usb",
                "Microsoft.SPOT.Hardware");
        }

        [Test]
        public void PuzzleSampleTest()
        {
            _typesOrder.AddRange(new[]
            {
                "<PrivateImplementationDetails>{143ADFEE-4589-467E-B091-DE1D534B7572}",
                "PuzzleSample.Resources",
                "PuzzleSample.Resources/BitmapResources",
                "PuzzleSample.MyPuzzle",
                "PuzzleSample.MyPuzzle/Button",
                "PuzzleSample.Resources/FontResources",
                "PuzzleSample.MyPuzzle/MyWindow",
                "PuzzleSample.MyPuzzle/PuzzleBoard",
                "PuzzleSample.Resources/StringResources",
                "<PrivateImplementationDetails>{143ADFEE-4589-467E-B091-DE1D534B7572}/__StaticArrayInitTypeSize=40"
            });
            TestSingleAssembly("Puzzle",
                "Microsoft.SPOT.Native", "Microsoft.SPOT.TinyCore", "Microsoft.SPOT.Graphics",
                "Microsoft.SPOT.Hardware", "Microsoft.SPOT.Ink", "Microsoft.SPOT.Touch");
        }

        private static void TestSingleAssembly(
            String name, params String[] dependencies)
        {
            var loadHints = dependencies
                .ToDictionary(item => item, item => String.Concat(@"..\..\Libs\", item, ".dll"));
            TestSingleAssembly(name, "le", loadHints, TinyBinaryWriter.CreateLittleEndianBinaryWriter);
            TestSingleAssembly(name, "be", loadHints, TinyBinaryWriter.CreateBigEndianBinaryWriter);
        }

        private static void TestSingleAssembly(
            String name, String endianness, IDictionary<String, String> loadHints,
            Func<BinaryWriter, TinyBinaryWriter> getBinaryWriter)
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(
                String.Format(@"Data\{0}\{0}.exe", Path.GetFileName(name)),
                new ReaderParameters { AssemblyResolver = new LoadHintsAssemblyResolver(loadHints)});

            var fileName = ProcessSingleFile(name, endianness,
                assemblyDefinition, getBinaryWriter);

            CompareBinaryFiles(fileName);
            CompareXmlFiles(
                String.Format(@"Data\{0}\{0}.pdbx", name),
                Path.ChangeExtension(fileName, "pdbx"));
        }

        private static String ProcessSingleFile(
            String name, String subDirectoryName,
            AssemblyDefinition assemblyDefinition,
            Func<BinaryWriter, TinyBinaryWriter> getBinaryWriter)
        {
            var peFileName = String.Format(@"Data\{0}\{1}\{0}.pex", name, subDirectoryName);
            var pdbxFileName = String.Format(@"Data\{0}\{1}\{0}.pdbx", name, subDirectoryName);

            var builder = new TinyAssemblyBuilder(assemblyDefinition, _typesOrder);

            using (var stream = File.Open(peFileName, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new BinaryWriter(stream))
            {
                builder.Write(getBinaryWriter(writer));
            }

            using (var writer = XmlWriter.Create(pdbxFileName,
                new XmlWriterSettings { Indent = true }))
            {
                builder.Write(writer);
            }

            return peFileName;
        }

        private static void CompareBinaryFiles(String name)
        {
            var expetedFileName = Path.ChangeExtension(name, ".pe");

            var expectedBytes = File.ReadAllBytes(expetedFileName);
            var realBytes = File.ReadAllBytes(name);

            Assert.AreEqual(expectedBytes.Length, realBytes.Length,
                "Size is not equal for file " + name);
            Assert.AreEqual(expectedBytes, realBytes,
                "Data is not equal for file " + name);
        }

        private static void CompareXmlFiles(
            String expected,
            String actual)
        {
            var tempExpected = Path.GetTempFileName();
            _pdbxSorter.Transform(expected, tempExpected);

            var tempActual = Path.GetTempFileName();
            _pdbxSorter.Transform(actual, tempActual);

            var xmlReaderSettings = new XmlReaderSettings { IgnoreWhitespace = true };
            using(var expectedReader = XmlReader.Create(tempExpected, xmlReaderSettings))
            using (var actualReader = XmlReader.Create(tempActual, xmlReaderSettings))
            {
                while (expectedReader.Read())
                {
                    actualReader.Read();

                    Assert.AreEqual(expectedReader.NodeType, actualReader.NodeType);
                    Assert.AreEqual(expectedReader.Name, actualReader.Name);
                    Assert.AreEqual(expectedReader.Value, actualReader.Value);
                }
            }

            File.Delete(tempExpected);
            File.Delete(tempActual);
        }
    }
}
