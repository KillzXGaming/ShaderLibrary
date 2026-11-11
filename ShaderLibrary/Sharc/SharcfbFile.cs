using ShaderLibrary.IO;
using ShaderLibrary.Sharc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static ShaderLibrary.SharcFile;

namespace ShaderLibrary
{
    public class SharcfbFile : ISharcFile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public uint Magic = 1397244226; // "BAHS";
            public uint Version = 9;
            public uint FileSize;
            public uint Unknown = 5;
            public uint ByteOrder; //0 or 1 for big endian
            public uint Alignment = 4096;
            public uint ShaderDataSize;
            public uint ShaderDataOffset;
        }

        public class ShaderVariation
        {
            public ShaderType Type;

            [XmlIgnore]
            public byte[] ByteCode;

            [XmlIgnore]
            public byte[] ControlShader;

            [XmlArray]
            public List<Symbol> Attributes = new List<Symbol>();
            [XmlArray]
            public List<Symbol> Samplers = new List<Symbol>();
            [XmlArray]
            public List<Symbol> Buffers = new List<Symbol>();
            [XmlArray]
            public List<Symbol> Uniforms = new List<Symbol>();
            [XmlArray]
            public List<SymbolUniformBlock> UniformBlocks = new();

            public string ToXml()
            {
                var serializer = new XmlSerializer(typeof(ShaderVariation));
                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, this);
                    return writer.ToString();
                }
            }
        }

        public class ShaderProgram : ISharcProgram
        {
            [XmlAttribute]
            public string Name { get; set; }
            [XmlAttribute]
            public int BaseIndex;
            [XmlAttribute]
            public int Kind = 3;

            [XmlArray]
            public List<SharcFile.VariationMacro> VariationMacros = new();

            public int GetVariationIndex(Dictionary<string, string> options)
                => SharcUtils.GetVariationIndex(this.VariationMacros, options);

            public int GetBinaryIndex(int variation)
                => BaseIndex + variation * (HasGeometryShader() ? 3 : 2);

            public IEnumerable<Dictionary<string, string>> GetAllVariationCombinations()
                => SharcUtils.GetAllVariationCombinations(this.VariationMacros);

            public bool HasGeometryShader() => (Kind & 4) != 0;
            public bool HasPixelShader() => (Kind & 2) != 0;
            public bool HasVertexShader() => (Kind & 1) != 0;
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
            public string Name { get; set; }
            public List<string> Values { get; set; } = new List<string>();
            public byte[] Data { get; set; }
        }
        public class Symbol
        {
            [XmlAttribute]
            public string Name;
            [XmlAttribute]
            public int Location;
        }
        public class SymbolUniformBlock
        {
            [XmlAttribute]
            public string Name;
            [XmlAttribute]
            public int Location;
            [XmlAttribute]
            public uint Size;
        }

        public Header FileHeader;
        public List<ShaderVariation> Variations = new();
        public List<ShaderProgram> Programs = new();
        public string Name;

        public bool IsSwitch = false;
        public bool IsVersion2 { get; set; }
        public static bool Identify(Stream stream)
        {
            using (var reader = new BinaryDataReader(stream, false, true)) {
                using (reader.BaseStream.TemporarySeek(0, SeekOrigin.Begin)) {
                    string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    return magic == "BAHS";
                }
            }
        }

        public IEnumerable<ISharcProgram> GetPrograms() => this.Programs;

        public SharcfbFile() {
            FileHeader = new Header()
            {

            };
        }
        public SharcfbFile(string filePath)
        {
            using (var reader = new BinaryDataReader(File.OpenRead(filePath)))
                SharcfbV2Reader.Read(this, reader);
        }

        public SharcfbFile(Stream stream)
        {
            using (var reader = new BinaryDataReader(stream))
                SharcfbV2Reader.Read(this, reader);
        }

        public void Save(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                Save(fs);
        }
        public void Save(Stream stream)
        {
            using (var wr = new BinaryDataWriter(stream))
            {
                if (IsSwitch)
                    SharcfbV2Writer.Write(this, wr);
                else
                    SharcfbV2Writer.Write(this, wr);
            }
        }

        static bool CheckIsSwitch(BinaryDataReader reader)
        {
            reader.SeekBegin(20);
            //check if this area is a valid alignment value
            //Other versions do not use this value
            bool isAlignment = reader.ReadUInt32() >= 2048;
            reader.BaseStream.Position = 0;
            return isAlignment;
        }

        public uint GetVersion()
        {
            return this.FileHeader.Version;
        }

        public enum ShaderType
        {
            Vertex,
            Pixel,
            Geometry,
        }

        internal class SharcfbV2Reader
        {
            private const uint STRING_TABLE_OFFSET = 0x150;

            public static void Read(SharcfbFile sharc, BinaryDataReader reader)
            {
                sharc.FileHeader = reader.ReadStruct<Header>();

                reader.SeekBegin(STRING_TABLE_OFFSET);
                sharc.Name = reader.ReadZeroTerminatedString();

                reader.SeekBegin(STRING_TABLE_OFFSET + sharc.FileHeader.Alignment);

                SharcReader.ReadSection(reader, sharc, (r, s) =>
                {
                    uint variationCount = reader.ReadUInt32();
                    for (int i = 0; i < variationCount; i++)
                        sharc.Variations.Add(SharcReader.ReadSection(reader, sharc, ReadVariation));
                });

                SharcReader.ReadSection(reader, sharc, (r, s) =>
                {
                    uint programCount = reader.ReadUInt32();
                    for (int i = 0; i < programCount; i++)
                        sharc.Programs.Add(SharcReader.ReadSection(reader, sharc, ReadProgram));
                });
            }

            static ShaderVariation ReadVariation(BinaryDataReader reader, ISharcFile sharc)
            {
                ShaderVariation variation = new();

                variation.Type = (ShaderType)reader.ReadUInt32();
                reader.ReadUInt32(); //0

                uint sectionSize = reader.ReadUInt32();
                var pos = reader.Position;

                long byteCodeOffset = ReadOffset(reader);
                uint controlCodeSize = reader.ReadUInt32();
                long controlCodeOffset = ReadOffset(reader);
                uint numUniformBlocks = reader.ReadUInt32();
                var uniformBlockOffset = ReadOffset(reader);

                uint numBuffers = 0;
                long bufferOffset = 0;

                if (sharc.IsVersion2)
                {
                    numBuffers = reader.ReadUInt32();
                    bufferOffset = ReadOffset(reader);
                }

                uint numAttributes = reader.ReadUInt32();
                var attributeOffset = ReadOffset(reader);
                uint numUniforms = reader.ReadUInt32();
                var uniformOffset = ReadOffset(reader);
                uint numSamplers = reader.ReadUInt32();
                var samplerOffset = ReadOffset(reader);

                reader.SeekBegin(pos + uniformBlockOffset);
                for (int i = 0; i < numUniformBlocks; i++)
                {
                    variation.UniformBlocks.Add(new SymbolUniformBlock()
                    {
                        Name = ReadString(reader),
                        Location = reader.ReadInt32(),
                        Size = reader.ReadUInt32(),
                    });
                }

                reader.SeekBegin(pos + attributeOffset);
                for (int i = 0; i < numAttributes; i++)
                {
                    variation.Attributes.Add(new Symbol()
                    {
                        Name = ReadString(reader),
                        Location = reader.ReadInt32(),
                    });
                }

                reader.SeekBegin((long)(pos + bufferOffset));
                for (int i = 0; i < numBuffers; i++)
                {
                    variation.Buffers.Add(new Symbol()
                    {
                        Name = ReadString(reader),
                        Location = reader.ReadInt32(),
                    });
                }

                reader.SeekBegin(pos + uniformOffset);
                for (int i = 0; i < numUniforms; i++)
                {
                    variation.Uniforms.Add(new Symbol()
                    {
                        Name = ReadString(reader),
                        Location = reader.ReadInt32(),
                    });
                }

                reader.SeekBegin(pos + samplerOffset);
                for (int i = 0; i < numSamplers; i++)
                {
                    variation.Samplers.Add(new Symbol()
                    {
                        Name = ReadString(reader),
                        Location = reader.ReadInt32(),
                    });
                }

                reader.SeekBegin(pos + controlCodeOffset);
                var bytes = reader.ReadBytes((int)controlCodeSize);
                var controlShader = new ControlShader(bytes);

                variation.ControlShader = bytes;

                reader.SeekBegin(((SharcfbFile)sharc).FileHeader.ShaderDataOffset + byteCodeOffset);
                variation.ByteCode = reader.ReadBytes((int)controlShader.GetByteCodeSize());

                return variation;
            }

            static ShaderProgram ReadProgram(BinaryDataReader reader, ISharcFile sharc)
            {
                ShaderProgram program = new();
                uint NameLength = reader.ReadUInt32();
                program.Kind = reader.ReadInt32(); //3
                program.BaseIndex = reader.ReadInt32();
                program.Name = reader.ReadFixedString((int)NameLength);
                program.VariationMacros = SharcReader.ReadSectionList(reader, sharc, SharcReader.ReadVariationMacro);
                return program;
            }

            static string ReadString(BinaryDataReader reader)
            {
                var offset = reader.ReadUInt64();
                using (reader.BaseStream.TemporarySeek(STRING_TABLE_OFFSET + (uint)offset, SeekOrigin.Begin))
                {
                    return reader.ReadZeroTerminatedString();
                }
            }
            static long ReadOffset(BinaryDataReader reader)
            {
                var offset = reader.ReadInt32();
                reader.ReadInt32();
                return offset;
            }
        }


        internal class SharcfbV2Writer
        {
            private const uint STRING_TABLE_OFFSET = 0x150;

            private static Dictionary<string, List<long>> _savedStrings = new();
            private static Dictionary<byte[], long> _savedControlCodes = new(new ByteArrayComparer());
            private static Dictionary<byte[], List<long>> _savedByteCodes = new(new ByteArrayComparer());

            public static void Write(SharcfbFile sharc, BinaryDataWriter writer)
            {
                _savedStrings.Add(sharc.Name, new List<long>());

                writer.WriteStruct(sharc.FileHeader);
                // For string table at the end
                writer.Seek((int)(STRING_TABLE_OFFSET + sharc.FileHeader.Alignment), SeekOrigin.Begin);

                // variation list
                WriteSection(writer, sharc, (wr, s) =>
                {
                    wr.Write(sharc.Variations.Count);
                    foreach (var program in sharc.Variations)
                        WriteSection(program, writer, sharc, WriteVariation);
                });
                // programs list
                WriteSection(writer, sharc, (wr, s) =>
                {
                    wr.Write(sharc.Programs.Count);
                    foreach (var program in sharc.Programs)
                        WriteSection(program, writer, sharc, WriteProgram, 0);
                }, 0);

                // Raw data
                writer.AlignBytes((int)sharc.FileHeader.Alignment);

                var start = writer.Position;
                sharc.FileHeader.ShaderDataOffset = (uint)start;

                foreach (var shader in _savedByteCodes)
                {
                    writer.AlignBytes((int)128);

                    var value = writer.Position - start;
                    writer.Write(shader.Key);
                    foreach (var ofs in shader.Value)
                    {
                        using (writer.BaseStream.TemporarySeek(ofs, SeekOrigin.Begin))
                            writer.Write((int)value);
                    }
                }
                writer.AlignBytes((int)sharc.FileHeader.Alignment);

                sharc.FileHeader.ShaderDataSize = (uint)(writer.Position - start);
                sharc.FileHeader.FileSize = (uint)writer.BaseStream.Length;

                // string table
                writer.Seek((int)STRING_TABLE_OFFSET, SeekOrigin.Begin);
                foreach (var str in _savedStrings)
                {
                    var value = writer.Position - STRING_TABLE_OFFSET;

                    writer.Write(Encoding.UTF8.GetBytes(str.Key));
                    writer.Write((byte)0);

                    foreach (var ofs in str.Value)
                    {
                        using (writer.BaseStream.TemporarySeek(ofs, SeekOrigin.Begin))
                            writer.Write((int)value);
                    }
                }

                writer.Seek(0, SeekOrigin.Begin);
                writer.WriteStruct(sharc.FileHeader);
            }

            static void WriteVariation(SharcfbFile.ShaderVariation variation, BinaryDataWriter wr, SharcfbFile sharc)
            {
                // header
                wr.Write((uint)variation.Type);
                wr.Write((uint)0);

                wr.Write((uint)0); // size set later

                // header data
                var controlShader = variation.ControlShader;
                var start = wr.Position;

                if (!_savedByteCodes.ContainsKey(variation.ByteCode))
                    _savedByteCodes.Add(variation.ByteCode, new List<long>());

                _savedByteCodes[variation.ByteCode].Add(wr.Position);

                wr.Write((ulong)0); // byte code offset relative to shader data
                wr.Write((uint)controlShader.Length);
                wr.Write((ulong)0); // control shader offset

                wr.Write(variation.UniformBlocks.Count);
                var uniformBlockOffset = wr.SaveOffset();

                long bufferOffset = 0;
                if (sharc.IsVersion2)
                {
                    wr.Write(variation.Buffers.Count);
                    bufferOffset = wr.SaveOffset();
                }

                wr.Write(variation.Attributes.Count);
                var attributeOffset = wr.SaveOffset();
                wr.Write(variation.Uniforms.Count);
                var uniformOffset = wr.SaveOffset();
                wr.Write(variation.Samplers.Count);
                var samplerOffset = wr.SaveOffset();

                wr.Write((ulong)0);
                wr.Write((uint)0);

                if (!_savedControlCodes.ContainsKey(controlShader))
                {
                    wr.Write((uint)0); // aligns by 8

                    WriteRelativeOffset(wr, start + 12, start);
                    _savedControlCodes.Add(controlShader, wr.Position);
                    wr.Write(controlShader);
                }
                else
                    WriteRelativeOffset(wr, _savedControlCodes[controlShader], start + 12, start);

                if (variation.UniformBlocks.Count > 0)
                {
                    WriteRelativeOffset(wr, uniformBlockOffset, start);
                    foreach (var symbol in variation.UniformBlocks)
                    {
                        SaveString(wr, symbol.Name);
                        wr.Write(symbol.Location);
                        wr.Write(symbol.Size);
                    }
                }

                if (variation.Attributes.Count > 0)
                {
                    WriteRelativeOffset(wr, attributeOffset, start);
                    foreach (var symbol in variation.Attributes)
                    {
                        SaveString(wr, symbol.Name);
                        wr.Write(symbol.Location);
                    }
                }
                if (variation.Buffers.Count > 0 && sharc.IsVersion2)
                {
                    WriteRelativeOffset(wr, bufferOffset, start);
                    foreach (var symbol in variation.Buffers)
                    {
                        SaveString(wr, symbol.Name);
                        wr.Write(symbol.Location);
                    }
                }

                if (variation.Uniforms.Count > 0)
                {
                    WriteRelativeOffset(wr, uniformOffset, start);
                    foreach (var symbol in variation.Uniforms)
                    {
                        SaveString(wr, symbol.Name);
                        wr.Write(symbol.Location);
                    }
                }
                if (variation.Samplers.Count > 0)
                {
                    WriteRelativeOffset(wr, samplerOffset, start);
                    foreach (var symbol in variation.Samplers)
                    {
                        SaveString(wr, symbol.Name);
                        wr.Write(symbol.Location);
                    }
                }

                wr.AlignBytes(8);

                var size = wr.Position - start;
                using (wr.BaseStream.TemporarySeek(start - 4, SeekOrigin.Begin))
                {
                    wr.Write((uint)size);
                }
            }

            static void WriteProgram(SharcfbFile.ShaderProgram program, BinaryDataWriter writer, SharcfbFile sharc)
            {
                writer.Write((uint)(program.Name.Length + 1));
                writer.Write((uint)program.Kind);
                writer.Write(program.BaseIndex);
                writer.Write(Encoding.UTF8.GetBytes(program.Name));
                writer.Write((byte)0);

                // macro list
                WriteSection(writer, sharc, (wr, s) =>
                {
                    wr.Write(program.VariationMacros.Count);
                    foreach (var macro in program.VariationMacros)
                        WriteSection(macro, writer, sharc, WriteMacro, 0);
                }, 0);
                // Unused lists
                WriteSection(writer, sharc, (wr, s) =>
                {
                    wr.Write(0);
                }, 0);
                WriteSection(writer, sharc, (wr, s) =>
                {
                    wr.Write(0);
                }, 0);
            }

            static void WriteMacro(SharcFile.VariationMacro macro, BinaryDataWriter writer, SharcfbFile sharc)
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

            static void WriteSection(BinaryDataWriter writer, SharcfbFile sharc, Action<BinaryDataWriter, SharcfbFile> section, int alignment = 4)
            {
                var start = writer.Position;
                writer.Write(0); // size set later
                section.Invoke(writer, sharc);
                if (alignment != 0)
                    writer.AlignBytes(alignment);
                var end = writer.Position;
                using (writer.BaseStream.TemporarySeek(start, SeekOrigin.Begin))
                {
                    writer.Write((uint)(end - start));
                }
            }

            static void WriteSection<T>(T value, BinaryDataWriter writer, SharcfbFile sharc, Action<T, BinaryDataWriter, SharcfbFile> section, int alignment = 4)
            {
                var start = writer.Position;
                writer.Write(0); // size set later
                section.Invoke(value, writer, sharc);

                if (alignment != 0)
                    writer.AlignBytes(alignment);
                var end = writer.Position;
                using (writer.BaseStream.TemporarySeek(start, SeekOrigin.Begin))
                {
                    writer.Write((uint)(end - start));
                }
            }

            static void WriteRelativeOffset(BinaryDataWriter writer, long target, long start)
            {
                var offset = writer.Position;
                WriteRelativeOffset(writer, offset, target, start);
            }

            static void WriteRelativeOffset(BinaryDataWriter writer, long offset, long target, long start)
            {
                var target_relative = offset - start;
                //Seek to where to write the offset itself and use relative position
                using (writer.BaseStream.TemporarySeek(target, SeekOrigin.Begin))
                {
                    writer.Write(((int)target_relative));
                }
            }


            static void SaveString(BinaryDataWriter writer, string str)
            {
                if (!_savedStrings.ContainsKey(str))
                    _savedStrings.Add(str, new List<long>());
                _savedStrings[str].Add(writer.Position);

                writer.Write(0UL);
            }

            class ByteArrayComparer : IEqualityComparer<byte[]>
            {
                public bool Equals(byte[] left, byte[] right)
                {
                    if (left == null || right == null)
                    {
                        return left == right;
                    }
                    return left.SequenceEqual(right);
                }
                public int GetHashCode(byte[] key)
                {
                    if (key == null)
                        throw new ArgumentNullException("key");
                    return key.Sum(b => b);
                }
            }
        }
    }
}