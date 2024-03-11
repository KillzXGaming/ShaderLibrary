using ShaderLibrary.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.Loading
{
    public class BfshaLoaderWiiU
    {
        public static void Load(BfshaFile bfsha, BinaryDataReader reader)
        {
            reader.SeekBegin(0);

            var header = new BfshaHeader();

            bfsha.Name = reader.LoadString(header.NameOffset);

            reader.SeekBegin(header.ShaderModelOffset);
            bfsha.ShaderModels = ReadDictionary(reader, ReadShaderModel);
        }

        static ShaderModel ReadShaderModel(BinaryDataReader reader)
        {
            ShaderModel shaderModel = new ShaderModel();

            var header = new ShaderModelHeader();


            return shaderModel;
        }

        static ResDict<T> ReadDictionary<T>(BinaryDataReader reader, Func<BinaryDataReader, T> load_section) where T : IResData, new()
        {
            ResDict<T> dict = new ResDict<T>();

            reader.ReadUInt32();
            int numNodes = reader.ReadInt32(); // Excludes root node.
            for (int i = 0; i < numNodes; i++)
            {
                var refe = reader.ReadUInt32();
                var IdxLeft = reader.ReadUInt16();
                var IdxRight = reader.ReadUInt16();
                var Key = reader.LoadString();
                var ValueOffset = reader.ReadOffset();

                if (ValueOffset == 0)
                    continue;

                using (reader.BaseStream.TemporarySeek(ValueOffset, SeekOrigin.Begin))
                {
                    dict.Add(Key, load_section(reader));
                }
            }
            return dict;
        }


        [StructLayout(LayoutKind.Sequential, Size = 0x10)]
        public struct BfshaHeader
        {
            public uint Magic;

            public byte VersionMajor;
            public byte VersionMajor2;
            public byte VersionMinor1;
            public byte VersionMinor2;

            public ushort ByteOrder;
            public ushort HeaderSize;
            public uint FileSize;
            public uint Alignment;
            public uint NameOffset;
            public uint StringPoolSize;
            public ulong StringPoolOffset;
            public ulong PathOffset;

            public ushort NumShaderModels;
            public ushort Flags;
            public uint Padding;

            public uint ShaderModelOffset;

            public uint Unknown1;
            public uint Unknown2;
        }

        [StructLayout(LayoutKind.Sequential, Size = 0x10)]
        public struct ShaderModelHeader
        {
            public byte StaticKeyLength;
            public byte DynamicKeyLength;
            public ushort StaticOptionCount;
            public ushort DynamicOptionCount;
            public ushort ProgramCount;
            public byte AttributeCount;
            public byte SamplerCount;
            public byte UniformBlockCount;
            public byte Unknown;

            public ushort Unknown2;
            public ushort Unknown3;
            public uint Unknown4;

            public uint UniformCount;
            public uint Unknown5;
            public uint DefaultProgramIndex;

            public uint NameOffset;
            public uint Unknown6;
            public uint Unknown7;

            public uint StaticOptionsOffset;
            public uint DynamicOptionsOffset;
            public uint AttributesOffset;
            public uint SamplersOffset;
            public uint UniformBlocksOffset;
            public uint UniformsOffset;
            public uint ShaderProgramsOffset;
            public uint KeyTableOffset;
            public uint ShaderArchiveOffset;
            public uint Unknown8;
            public uint Unknown9;
            public uint SymbolsOffset;
        }

        [StructLayout(LayoutKind.Sequential, Size = 0x10)]
        public struct ShaderOptionHeader
        {
            public byte ChoiceCount;
            public byte DefaultIndex;
            public ushort BlockOffset; // Uniform block offset.

            public byte flag;
            public byte KeyOffset;
            public byte Bit32Index;
            public byte Bit32Shift;

            public uint bit32Mask;
            public uint NameOffset;
            public uint ChoicesOffset;
            public uint ChoicesValuesOffset;
        }

        [StructLayout(LayoutKind.Sequential, Size = 0x10)]
        public struct AttributeHeader
        {
            public byte Index;
            public byte GX2Type;
            public byte GX2Count;
            public byte Location;
        }

        [StructLayout(LayoutKind.Sequential, Size = 0x10)]
        public struct SamplerHeader
        {
            public byte Index;
            public byte GX2Type;
            public byte GX2Count;
            public byte Padding;
            public uint AnnNameOffset;
        }

        [StructLayout(LayoutKind.Sequential, Size = 0x10)]
        public struct ShaderProgramHeader
        {
            public ushort Unknown;
            public byte SamplerCount;
            public byte BlockCount;
            public uint UsedAttributeFlags;

            public uint Unknown2;
            public uint Unknown3;
            public uint Unknown4;
            public uint Unknown5;
            public uint Unknown6;
            public uint Unknown7;

            public uint SamplerLocationsOffset;
            public uint UniformBlockLocationsOffset;

            public uint GX2VertexDataOffset;
            public uint GX2GeometryDataOffset;
            public uint GX2FragmentDataOffset;

            public uint ParentModelOffset;
        }
    }
}
