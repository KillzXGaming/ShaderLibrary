using ShaderLibrary.IO;
using ShaderLibrary.Sharc;
using System.Runtime.InteropServices;
using System.Text;
using static ShaderLibrary.SharcFile;

namespace ShaderLibrary
{
    public class SharcfbFileWiiU : ISharcFile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public uint Magic = 1397244226; // "BAHS";
            public uint Version = 8;
            public uint FileSize;
            public uint ByteOrder = 1; //0 or 1 for big endian
            public uint Zero;
        }   

        public class ShaderBinary
        {
            public GX2ShaderType Type;
            public byte[] Data;
        }

        public class ShaderProgram : ISharcProgram
        {
            public string Name { get; set; }
            public uint Kind = 3;
            public int BaseIndex;
            public int StageCount => HasGeometryShader() ? 3 : 2;

            public List<SharcFile.VariationMacro> VariationMacros = new();
            public List<SharcFile.VariationMacro> VariationDefaults = new();
            public List<SharcFile.Symbol> Uniforms = new();
            public List<SharcFile.Symbol> UniformBlocks = new();
            public List<SharcFile.Symbol> Samplers = new();
            public List<SharcFile.Symbol> Attributes = new();

            public int GetBinaryIndex(Dictionary<string, string> options)
            {
                var variant = GetVariationIndex(options);
                return GetBinaryIndex(variant);
            }

            public int GetBinaryIndex(int variant)
                => BaseIndex + variant * StageCount;
            public int GetVariationIndex(Dictionary<string, string> options)
                => SharcUtils.GetVariationIndex(this.VariationMacros, options);
            public IEnumerable<Dictionary<string, string>> GetAllVariationCombinations()
                => SharcUtils.GetAllVariationCombinations(this.VariationMacros);

            public bool HasGeometryShader() => (Kind & 4) != 0;
            public bool HasPixelShader() => (Kind & 2) != 0;
            public bool HasVertexShader() => (Kind & 1) != 0;

            public IEnumerable<SharcFile.VariationMacro> GetMacros() => VariationMacros;
            public string ToXml() => "";
        }

        public class VariationMacro
        {
            public string Name { get; set; }
            public List<string> Values { get; set; } = new List<string>();
            public byte[] Data { get; set; }
        }
        public class Symbol
        {
            public uint Size;
            public string Name;
            public string SymbolName;
            public List<string> Values = new();
            public byte[] DefaultData;
            public byte[] UsedVariants;
        }
        public class SymbolUniformBlock
        {
            public string Name;
            public int Location;
            public uint Size;
        }

        public Header FileHeader;
        public List<ShaderProgram> Programs = new();
        public List<ShaderBinary> Binaries = new();
        public string Name;

        public bool IsSwitch = false;

        public IEnumerable<ISharcProgram> GetPrograms() => this.Programs;

        public SharcfbFileWiiU() {
            this.FileHeader = new Header()
            {
            };
        }

        public SharcfbFileWiiU(string filePath) {
            using (var reader = new BinaryDataReader(File.OpenRead(filePath)))
                SharcfbV1Reader.Read(this, reader);
        }
        public SharcfbFileWiiU(Stream stream)
        {
            using (var reader = new BinaryDataReader(stream))
                SharcfbV1Reader.Read(this, reader);
        }


        public void Save(Stream stream)
        {
            using (var wr = new BinaryDataWriter(stream))
                SharcfbV1Writer.Write(this, wr);
        }

        public void Save(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var wr = new BinaryDataWriter(fs))
                SharcfbV1Writer.Write(this, wr);
        }

        public uint GetVersion()
        {
            return this.FileHeader.Version;
        }

        public enum GX2ShaderType
        {
            Vertex,
            Pixel,
            Geometry,
        }

        internal class SharcfbV1Reader
        {
            public static void Read(SharcfbFileWiiU sharc, BinaryDataReader reader)
            {
                sharc.FileHeader = reader.ReadStruct<Header>();

                uint nameLength = reader.ReadUInt32();
                sharc.Name = reader.ReadFixedString((int)nameLength);

                SharcReader.ReadSection(reader, sharc, (r, s) =>
                {
                    uint binaryCount = reader.ReadUInt32();
                    for (int i = 0; i < binaryCount; i++)
                        sharc.Binaries.Add(SharcReader.ReadSection(reader, sharc, ReadShaderBinary));
                });
                SharcReader.ReadSection(reader, sharc, (r, s) =>
                {
                    uint programCount = reader.ReadUInt32();
                    for (int i = 0; i < programCount; i++)
                        sharc.Programs.Add(SharcReader.ReadSection(reader, sharc, ReadShaderProgram));
                });
            }

            static ShaderBinary ReadShaderBinary(BinaryDataReader reader, ISharcFile sharc)
            {
                ShaderBinary bin = new();
                bin.Type = (GX2ShaderType)reader.ReadUInt32();
                uint offset = reader.ReadUInt32();
                // Trace.Assert(offset == 0);

                uint size = reader.ReadUInt32();
                bin.Data = reader.ReadBytes((int)size);
                return bin;
            }

            static ShaderProgram ReadShaderProgram(BinaryDataReader reader, ISharcFile sharc)
            {
                ShaderProgram program = new();
                uint nameLength = reader.ReadUInt32();
                program.Kind = reader.ReadUInt32();
                program.BaseIndex = reader.ReadInt32();
                program.Name = reader.ReadFixedString((int)nameLength);
                program.VariationMacros = SharcReader.ReadSectionList(reader, sharc, SharcReader.ReadVariationMacro);
                program.VariationDefaults = SharcReader.ReadSectionList(reader, sharc, SharcReader.ReadVariationMacro);
                program.Uniforms = SharcReader.ReadSectionList(reader, sharc, ReadSymbol);
                if (sharc.GetVersion() >= 9)
                {
                    uint size = reader.ReadUInt32();
                    reader.SeekBegin(reader.Position - 4 + size);
                }
                program.UniformBlocks = SharcReader.ReadSectionList(reader, sharc, ReadSymbol);
                program.Samplers = SharcReader.ReadSectionList(reader, sharc, ReadSymbol);
                program.Attributes = SharcReader.ReadSectionList(reader, sharc, ReadSymbol);
                return program;
            }

            static SharcFile.Symbol ReadSymbol(BinaryDataReader reader, ISharcFile sharc)
            {
                SharcFile.Symbol var = new();
                var.Size = reader.ReadUInt32();
                uint variableNameLength = reader.ReadUInt32();
                uint symbolNameLength = reader.ReadUInt32();
                uint defaultSize = reader.ReadUInt32();
                uint numVariations = reader.ReadUInt32();

                var.Name = reader.ReadFixedString((int)variableNameLength);
                var.SymbolName = reader.ReadFixedString((int)symbolNameLength);
                var.DefaultData = reader.ReadBytes((int)defaultSize);
                var.UsedVariants = reader.ReadBytes((int)numVariations);
                return var;
            }
        }

        internal class SharcfbV1Writer
        {
            public static void Write(SharcfbFileWiiU sharc, BinaryDataWriter writer)
            {
                writer.WriteStruct(sharc.FileHeader);
                writer.Write((uint)(sharc.Name.Length + 1));
                writer.Write(Encoding.UTF8.GetBytes(sharc.Name));
                writer.Write((byte)0);

                WriteSection(writer, sharc, (wr, s) =>
                {
                    wr.Write(sharc.Binaries.Count);
                    foreach (var binary in sharc.Binaries)
                        WriteSection(binary, wr, sharc, WriteShaderBinary);
                });
                WriteSection(writer, sharc, (wr, s) =>
                {
                    wr.Write(sharc.Programs.Count);
                    foreach (var program in sharc.Programs)
                        WriteSection(program, wr, sharc, WriteShaderProgram);
                });
            }

            static void WriteShaderBinary(SharcfbFileWiiU.ShaderBinary binary, BinaryWriter writer, SharcfbFileWiiU sharc)
            {
                writer.Write((uint)binary.Type);
                writer.Write(0); // offset
                writer.Write((uint)binary.Data.Length);
                writer.Write(binary.Data);
            }
            static void WriteShaderProgram(SharcfbFileWiiU.ShaderProgram binary, BinaryDataWriter writer, SharcfbFileWiiU sharc)
            {
                writer.Write((uint)(binary.Name.Length + 1));
                writer.Write(binary.Kind);
                writer.Write(binary.BaseIndex);
                writer.Write(Encoding.UTF8.GetBytes(binary.Name));
                writer.Write((byte)0);

                WriteMacros(binary.VariationMacros, writer, sharc);
                WriteMacros(binary.VariationDefaults, writer, sharc);
                WriteSymbols(binary.Uniforms, writer, sharc);
                WriteSymbols(binary.UniformBlocks, writer, sharc);
                WriteSymbols(binary.Samplers, writer, sharc);
                WriteSymbols(binary.Attributes, writer, sharc);
            }

            static void WriteMacros(List<SharcFile.VariationMacro> macros, BinaryDataWriter writer, SharcfbFileWiiU sharc)
            {
                WriteSection(writer, sharc, (wr, s) =>
                {
                    wr.Write(macros.Count);
                    foreach (var macro in macros)
                        WriteSection(macro, wr, sharc, WriteMacro);
                });
            }

            static void WriteSymbols(List<SharcFile.Symbol> symbols, BinaryDataWriter writer, SharcfbFileWiiU sharc)
            {
                WriteSection(writer, sharc, (wr, s) =>
                {
                    wr.Write(symbols.Count);
                    foreach (var symbol in symbols)
                        WriteSection(symbol, wr, sharc, WriteSymbol);
                });
            }

            static void WriteMacro(SharcFile.VariationMacro macro, BinaryWriter writer, SharcfbFileWiiU sharc)
            {
                writer.Write((uint)(macro.Name.Length + 1));
                writer.Write((uint)macro.Values.Count);
                writer.Write((uint)macro.Data.Length);
                writer.Write(Encoding.UTF8.GetBytes(macro.Name));
                writer.Write((byte)0);

                foreach (var v in macro.Values)
                {
                    writer.Write(Encoding.UTF8.GetBytes(v));
                    writer.Write((byte)0);
                }
                writer.Write(macro.Data);
            }
            static void WriteSymbol(SharcFile.Symbol symbol, BinaryWriter writer, SharcfbFileWiiU sharc)
            {
                writer.Write(symbol.Size);

                writer.Write((uint)(symbol.Name.Length + 1));
                writer.Write((uint)(symbol.SymbolName.Length + 1));
                writer.Write((uint)(symbol.DefaultData.Length));
                writer.Write((uint)(symbol.UsedVariants.Length));

                writer.Write(Encoding.UTF8.GetBytes(symbol.Name));
                writer.Write((byte)0);
                writer.Write(Encoding.UTF8.GetBytes(symbol.SymbolName));
                writer.Write((byte)0);
                writer.Write(symbol.DefaultData);
                writer.Write(symbol.UsedVariants);
            }

            static void WriteSection(BinaryDataWriter writer, SharcfbFileWiiU sharc, Action<BinaryDataWriter, SharcfbFileWiiU> section)
            {
                var start = writer.Position;
                writer.Write(0); // size set later
                section.Invoke(writer, sharc);
                var end = writer.Position;
                using (writer.BaseStream.TemporarySeek(start, SeekOrigin.Begin))
                {
                    writer.Write((uint)(end - start));
                }
            }

            static void WriteSection<T>(T value, BinaryDataWriter writer, SharcfbFileWiiU sharc, Action<T, BinaryDataWriter, SharcfbFileWiiU> section)
            {
                var start = writer.Position;
                writer.Write(0); // size set later
                section.Invoke(value, writer, sharc);
                var end = writer.Position;
                using (writer.BaseStream.TemporarySeek(start, SeekOrigin.Begin))
                {
                    writer.Write((uint)(end - start));
                }
            }
        }
    }
}