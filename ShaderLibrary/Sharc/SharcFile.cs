using ShaderLibrary.Helpers;
using ShaderLibrary.IO;
using ShaderLibrary.Sharc;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;
using static ShaderLibrary.SharcFile;

namespace ShaderLibrary
{
    public class SharcFile : ISharcFile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public uint Magic = 1397244225; // "AAHS";
            public uint Version = 13;
            public uint FileSize;
            public uint ByteOrder = 1; // 0 for big, 1 for little
        }

        public class ShaderSource
        {
            public string Name;
            public byte[] Data;

            public ShaderSource() { }
            public ShaderSource(string source, string name) {
                SetCode(source);
                Name = name;
            }
            public string GetCode()
            {
                var encoding = TextEncoding.ShiftJIS();
                var code = encoding.GetString(Data);
                code = code.TrimEnd('\0');
                return code.Replace("・ｿ", ""); // remove bom
            }

            public void SetCode(string source)
            {
                source ??= string.Empty;
                var encoding = TextEncoding.ShiftJIS();
                // Add terminator
                if (!source.EndsWith("\0"))
                    source += '\0';
                this.Data = encoding.GetBytes(source);
            }
        }

        public class MacroDefine
        {
            [XmlAttribute("name")]
            public string Name;
            [XmlAttribute("value")]
            public string Value;
        }

        public class ShaderProgram : ISharcProgram
        {
            public string Name { get; set; }

            public List<MacroDefine> VertexMacros = new List<MacroDefine>();
            public List<MacroDefine> FragmentMacros = new List<MacroDefine>();
            public List<MacroDefine> GeometryMacros = new List<MacroDefine>();
            public List<MacroDefine> ComputeMacros = new List<MacroDefine>();

            public List<VariationMacro> VariationMacros = new List<VariationMacro>();
            public List<VariationMacro> VariationDefaults = new List<VariationMacro>();

            public List<Symbol> Uniforms = new List<Symbol>();
            public List<Symbol> UniformBlocks = new List<Symbol>();
            public List<Symbol> Samplers = new List<Symbol>();
            public List<Symbol> Attributes = new List<Symbol>();

            public List<SymbolUniformBlock> UniformBlocksV13 = new List<SymbolUniformBlock>();

            public int VertexShaderIndex = -1;
            public int FragmentShaderIndex = -1;
            public int GeoemetryShaderIndex = -1;
            public int ComputeShaderIndex = -1;

            public int GetBinaryIndex(int variation) => -1;

            public int GetVariationIndex(Dictionary<string, string> options)
                => SharcUtils.GetVariationIndex(this.VariationMacros, options);

            public IEnumerable<Dictionary<string, string>> GetAllVariationCombinations()
                => SharcUtils.GetAllVariationCombinations(this.VariationMacros);

            public IEnumerable<SharcFile.VariationMacro> GetMacros() => VariationMacros;

            public string ToXml()
            {
                var serializer = new XmlSerializer(typeof(ShaderProgram));
                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, this);
                    return writer.ToString();
                }
            }
        }

        public class VariationMacro
        {
            [XmlAttribute("name")]
            public string Name { get; set; }
            [XmlAttribute("values")]
            public List<string> Values { get; set; } = new List<string>();
            [XmlAttribute("symbol")]
            public byte[] Data { get; set; }
        }

        public class Symbol
        {
            [XmlAttribute("offset")]
            public uint Offset;
            [XmlAttribute("size")]
            public uint Size;
            [XmlAttribute("name")]
            public string Name;
            [XmlAttribute("symbol")]
            public string SymbolName;
            [XmlAttribute("values")]
            public List<string> Values = new();
            [XmlIgnore]
            [XmlElement("default_data")]
            public byte[] DefaultData;
            [XmlElement("used_variants")]
            public byte[] UsedVariants;
        }
        public class SymbolUniformBlock
        {
            [XmlAttribute("name")]
            public string Name;
            [XmlAttribute("location")]
            public int Location;
            [XmlAttribute("size")]
            public uint Size;

            // For sharc
            [XmlArray("uniforms")]
            [XmlArrayItem("uniform")]
            public List<Symbol> Uniforms = new();
        }

        public Header FileHeader;
        public List<ShaderProgram> Programs = new();
        public List<ShaderSource> Sources = new();

        public string Name { get; set; }
        public uint Version
        {
            get => this.FileHeader.Version;
            set => this.FileHeader.Version = value;
        }
        public bool IsLittleEndian
        {
            get => this.FileHeader.ByteOrder == 1;
            set => this.FileHeader.ByteOrder = value ? 1u : 0;
        }

        public bool IsSwitch = false;

        public uint GetVersion() => this.FileHeader.Version;
        public IEnumerable<ISharcProgram> GetPrograms() => this.Programs;

        public static bool Identify(Stream stream)
        {
            using (var reader = new BinaryDataReader(stream, false, true)) {
                using (reader.BaseStream.TemporarySeek(0, SeekOrigin.Begin)) {
                    string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    return magic == "AAHS";
                }
            }
        }

        public SharcFile() { FileHeader = new Header(); }
        public SharcFile(string filePath)
        {
            using (var reader = new BinaryDataReader(File.OpenRead(filePath)))
                SharcReader.Read(this, reader);
        }
        public SharcFile(Stream stream)
        {
            using (var reader = new BinaryDataReader(stream))
                SharcReader.Read(this, reader);
        }

        public void Save(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                Save(fs);
        }
        public void Save(Stream stream)
        {
            using (var writer = new BinaryDataWriter(stream))
                SharcWriter.Write(this, writer);
        }

        internal class SharcReader
        {
            public static void Read(SharcFile sharc, BinaryDataReader reader)
            {
                sharc.FileHeader = reader.ReadStruct<Header>();

                uint nameLength = reader.ReadUInt32();
                sharc.Name = reader.ReadFixedString((int)nameLength);
                ReadSection(reader, sharc, (r, s) =>
                {
                    uint programCount = reader.ReadUInt32();
                    for (int i = 0; i < programCount; i++)
                        sharc.Programs.Add(ReadSection(reader, sharc, ReadShaderProgram));
                });
                ReadSection(reader, sharc, (r, s) =>
                {
                    uint sourceCount = reader.ReadUInt32();
                    for (int i = 0; i < sourceCount; i++)
                        sharc.Sources.Add(ReadSection(reader, sharc, ReadShaderSource));
                });
            }

            static ShaderSource ReadShaderSource(BinaryDataReader reader, ISharcFile sharc)
            {
                ShaderSource src = new();
                uint nameLength = reader.ReadUInt32();
                uint codeLength = reader.ReadUInt32();
                uint codeLength2 = reader.ReadUInt32();
                src.Name = reader.ReadFixedString((int)nameLength);
                src.Data = reader.ReadBytes((int)codeLength);
                return src;
            }

            static ShaderProgram ReadShaderProgram(BinaryDataReader reader, ISharcFile sharc)
            {
                ShaderProgram program = new();
                uint nameLength = reader.ReadUInt32();


                if (sharc.GetVersion() >= 13)
                {
                    program.VertexShaderIndex = reader.ReadInt16();
                    program.ComputeShaderIndex = reader.ReadInt16();
                    program.FragmentShaderIndex = reader.ReadInt16();
                    program.GeoemetryShaderIndex = reader.ReadInt16();
                    reader.ReadInt16();
                    reader.ReadInt16();
                    reader.ReadInt16();
                    reader.ReadInt16();

                    // Trace.Assert(reader.ReadInt16() == -1);
                    //Trace.Assert(reader.ReadInt16() == -1);
                    //Trace.Assert(reader.ReadInt16() == -1);
                    //Trace.Assert(reader.ReadInt16() == -1);
                }
                else
                {
                    program.VertexShaderIndex = reader.ReadInt32();
                    program.FragmentShaderIndex = reader.ReadInt32();
                    program.GeoemetryShaderIndex = reader.ReadInt32();
                }

                program.Name = reader.ReadFixedString((int)nameLength);
                program.VertexMacros = ReadSectionList(reader, sharc, ReadMacroDefine);
                program.FragmentMacros = ReadSectionList(reader, sharc, ReadMacroDefine);
                program.GeometryMacros = ReadSectionList(reader, sharc, ReadMacroDefine);
                if (sharc.GetVersion() >= 13)
                    program.ComputeMacros = ReadSectionList(reader, sharc, ReadMacroDefine);

                program.VariationMacros = ReadSectionList(reader, sharc, ReadVariationMacro);
                program.VariationDefaults = ReadSectionList(reader, sharc, ReadVariationMacro);
                if (sharc.GetVersion() >= 13)
                {
                    program.UniformBlocksV13 = ReadSectionList(reader, sharc, ReadBlockSymbol);
                }
                else
                {
                    program.Uniforms = ReadSectionList(reader, sharc, ReadSymbol);
                    program.UniformBlocks = ReadSectionList(reader, sharc, ReadSymbol);
                    program.Samplers = ReadSectionList(reader, sharc, ReadSymbol);
                    program.Attributes = ReadSectionList(reader, sharc, ReadSymbol);
                }
                return program;
            }

            static MacroDefine ReadMacroDefine(BinaryDataReader reader, ISharcFile sharc)
            {
                MacroDefine var = new();
                uint nameLength = reader.ReadUInt32();
                uint valueLength = reader.ReadUInt32();
                var.Name = reader.ReadFixedString((int)nameLength);
                var.Value = reader.ReadFixedString((int)valueLength);
                return var;
            }

            public static VariationMacro ReadVariationMacro(BinaryDataReader reader, ISharcFile sharc)
            {
                VariationMacro var = new();
                uint nameLength = reader.ReadUInt32();
                uint valueCount = reader.ReadUInt32();
                uint dataLength = reader.ReadUInt32();
                var.Name = reader.ReadFixedString((int)nameLength);
                for (int i = 0; i < valueCount; i++)
                    var.Values.Add(reader.ReadZeroTerminatedString());
                var.Data = reader.ReadBytes((int)dataLength); // 0
                return var;
            }

            static SymbolUniformBlock ReadBlockSymbol(BinaryDataReader reader, ISharcFile sharc)
            {
                SymbolUniformBlock var = new();
                var.Size = reader.ReadUInt32();
                var.Name = reader.ReadFixedString((int)reader.ReadUInt32());
                uint subSectionSize = reader.ReadUInt32();
                uint uniformCount = reader.ReadUInt32();
                for (int i = 0; i < uniformCount; i++)
                {
                    ReadSection(reader, sharc, (wr, s) =>
                    {
                        Symbol uniform = new();
                        uniform.Offset = reader.ReadUInt32();
                        uniform.Name = reader.ReadFixedString((int)reader.ReadUInt32());
                        var.Uniforms.Add(uniform);
                    });
                }
                return var;
            }

            static Symbol ReadSymbol(BinaryDataReader reader, ISharcFile sharc)
            {
                Symbol var = new();
                var.Offset = reader.ReadUInt32();
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

            public static List<T> ReadSectionList<T>(BinaryDataReader reader, ISharcFile sharc, Func<BinaryDataReader, ISharcFile, T> sectionReader)
            {
                var list = new List<T>();

                long start = reader.Position;
                var sectionSize = reader.ReadUInt32();
                uint count = reader.ReadUInt32();
                for (int i = 0; i < count; i++)
                    list.Add(ReadSection(reader, sharc, sectionReader));

                reader.SeekBegin(start + sectionSize);
                return list;
            }

            public static T ReadSection<T>(BinaryDataReader reader, ISharcFile sharc, Func<BinaryDataReader, ISharcFile, T> sectionReader)
            {
                long start = reader.Position;
                var sectionSize = reader.ReadUInt32();
                T value = sectionReader.Invoke(reader, sharc);

                reader.SeekBegin(start + sectionSize);
                return value;
            }

            public static void ReadSection(BinaryDataReader reader, ISharcFile sharc, Action<BinaryDataReader, ISharcFile> section)
            {
                long start = reader.Position;
                var sectionSize = reader.ReadUInt32();
                section.Invoke(reader, sharc);

                reader.SeekBegin(start + sectionSize);
            }
        }

        internal class SharcWriter
        {
            public static void Write(SharcFile sharc, BinaryDataWriter writer)
            {
                writer.Version = (int)sharc.GetVersion();

                var header = sharc.FileHeader ?? new SharcFile.Header();
                writer.WriteStruct(header);
                WriteStringWithLength(writer, sharc.Name ?? "Unnamed");

                // Programs
                WriteSectionList(writer, sharc.Programs, WriteShaderProgram);
                // Sources
                WriteSectionList(writer, sharc.Sources, WriteShaderSource);
                // File size
                header.FileSize = (uint)writer.BaseStream.Length;
                using (writer.BaseStream.TemporarySeek(0, SeekOrigin.Begin)) {
                    writer.WriteStruct(header);
                }
            }

            private static void WriteShaderSource(BinaryDataWriter writer, SharcFile.ShaderSource src)
            {
                byte[] nameBytes = GetUtf8ZeroTerminated(src.Name);
                byte[] data = src.Data ?? Array.Empty<byte>();

                writer.Write((uint)nameBytes.Length);
                writer.Write((uint)data.Length);
                writer.Write((uint)data.Length); // same as code length 1
                writer.Write(nameBytes);
                writer.Write(data);
            }
            private static void WriteShaderProgram(BinaryDataWriter writer, SharcFile.ShaderProgram prog)
            {
                byte[] nameBytes = GetUtf8ZeroTerminated(prog.Name);
                writer.Write((uint)nameBytes.Length);

                bool isV13 = writer.Version >= 13;
                if (isV13)
                {
                    writer.Write((short)prog.VertexShaderIndex);
                    writer.Write((short)prog.ComputeShaderIndex);
                    writer.Write((short)prog.FragmentShaderIndex);
                    writer.Write((short)prog.GeoemetryShaderIndex);

                    // 4 unknown shorts (usually -1)
                    for (int i = 0; i < 4; i++)
                        writer.Write((short)-1);
                }
                else
                {
                    writer.Write(prog.VertexShaderIndex);
                    writer.Write(prog.FragmentShaderIndex);
                    writer.Write(prog.GeoemetryShaderIndex);
                }
                writer.Write(nameBytes);

                // Macros
                WriteSectionList(writer, prog.VertexMacros, WriteMacroDefine);
                WriteSectionList(writer, prog.FragmentMacros, WriteMacroDefine);
                WriteSectionList(writer, prog.GeometryMacros, WriteMacroDefine);
                if (isV13)
                    WriteSectionList(writer, prog.ComputeMacros, WriteMacroDefine);

                WriteSectionList(writer,  prog.VariationMacros, WriteVariationMacro);
                WriteSectionList(writer, prog.VariationDefaults, WriteVariationMacro);

                // Symbols
                if (isV13)
                {
                    WriteSectionList(writer, prog.UniformBlocksV13, WriteSymbolUniformBlock);
                }
                else
                {
                    WriteSectionList(writer, prog.Uniforms, WriteSymbol);
                    WriteSectionList(writer, prog.UniformBlocks, WriteSymbol);
                    WriteSectionList(writer, prog.Samplers, WriteSymbol);
                    WriteSectionList(writer, prog.Attributes, WriteSymbol);
                }
            }

            private static void WriteMacroDefine(BinaryDataWriter writer, SharcFile.MacroDefine macro)
            {
                byte[] macroBytes = GetUtf8ZeroTerminated(macro.Name);
                byte[] valuesBytes = GetUtf8ZeroTerminated(macro.Value);
                writer.Write((uint)macroBytes.Length);
                writer.Write((uint)valuesBytes.Length);
                writer.Write(macroBytes);
                writer.Write(valuesBytes);
            }

            private static void WriteVariationMacro(BinaryDataWriter writer, SharcFile.VariationMacro macro)
            {
                byte[] nameBytes = GetUtf8ZeroTerminated(macro.Name);

                writer.Write((uint)nameBytes.Length);
                writer.Write((uint)macro.Values.Count);
                writer.Write((uint)(macro.Data?.Length ?? 0));
                writer.Write(nameBytes);
                foreach (var val in macro.Values)
                {
                    writer.Write(Encoding.UTF8.GetBytes(val));
                    writer.Write((byte)0);
                }
                writer.Write(macro.Data ?? Array.Empty<byte>());
            }

            private static void WriteSymbol(BinaryDataWriter writer, SharcFile.Symbol sym)
            {
                byte[] nameBytes = GetUtf8ZeroTerminated(sym.Name);
                byte[] symbolBytes = GetUtf8ZeroTerminated(sym.SymbolName);

                writer.Write(sym.Offset);
                writer.Write((uint)nameBytes.Length);
                writer.Write((uint)symbolBytes.Length);
                writer.Write((uint)(sym.DefaultData?.Length ?? 0));
                writer.Write((uint)(sym.UsedVariants?.Length ?? 0));

                writer.Write(nameBytes);
                writer.Write(symbolBytes);
                writer.Write(sym.DefaultData ?? Array.Empty<byte>());
                writer.Write(sym.UsedVariants ?? Array.Empty<byte>());
            }

            private static void WriteSymbolUniformBlock(BinaryDataWriter writer, SharcFile.SymbolUniformBlock block)
            {
                writer.Write(block.Size);
                WriteStringWithLength(writer, block.Name ?? "");
                WriteSectionList(writer, block.Uniforms, WriteSymbolUniform);
            }

            private static void WriteSymbolUniform(BinaryDataWriter writer, SharcFile.Symbol uniform)
            {
                writer.Write(uniform.Offset);
                WriteStringWithLength(writer, uniform.Name ?? "");
            }

            private static void WriteStringWithLength(BinaryDataWriter writer, string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    writer.Write(0u);
                    return;
                }
                byte[] bytes = GetUtf8ZeroTerminated(str);
                writer.Write((uint)bytes.Length);
                writer.Write(bytes);
            }

            static void WriteSection(BinaryDataWriter writer, Action<BinaryDataWriter> section, int alignment = 4)
            {
                var start = writer.Position;
                writer.Write(0); // size set later
                section.Invoke(writer);
                if (alignment != 0)
                    writer.AlignBytes(alignment);
                var end = writer.Position;
                using (writer.BaseStream.TemporarySeek(start, SeekOrigin.Begin))
                {
                    writer.Write((uint)(end - start));
                }
            }

            static void WriteSection<T>(T value, BinaryDataWriter writer, Action<BinaryDataWriter, T> section, int alignment = 4)
            {
                var start = writer.Position;
                writer.Write(0); // size set later
                section.Invoke(writer, value);

                if (alignment != 0)
                    writer.AlignBytes(alignment);
                var end = writer.Position;
                using (writer.BaseStream.TemporarySeek(start, SeekOrigin.Begin))
                {
                    writer.Write((uint)(end - start));
                }
            }

            private static void WriteSectionList<T>(BinaryDataWriter writer, List<T> list, Action<BinaryDataWriter, T> writeItem)
            {
                WriteSection(writer, (wr) =>
                {
                    wr.Write(list.Count);
                    foreach (var item in list)
                        WriteSection(item, writer, writeItem, 0);
                }, 0);
            }

            private static byte[] GetUtf8ZeroTerminated(string str)
            {
                return Encoding.UTF8.GetBytes((str ?? "") + "\0");
            }
        }
    }
}