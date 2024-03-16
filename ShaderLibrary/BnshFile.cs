using ShaderLibrary.IO;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary
{
    public class BnshFile
    {
        public string Name { get; set; }

        public List<ShaderVariation> Variations { get; set; }

        public BinaryHeader BinHeader; //A header shared between bnsh and other formats
        public BnshHeader Header; //Bnsh header

        public BnshFile() { }

        public BnshFile(string filePath)
        {
            Read(File.OpenRead(filePath));
        }

        public BnshFile(Stream stream)
        {
            Read(stream);
        }

        public void Save(string  filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                Save(fs);
        }

        public void Save(Stream stream)
        {
            BnshSaver saver = new BnshSaver();
            using (var writer = new BinaryDataWriter(stream))
                saver.Save(this, writer);
        }

        public void Read(Stream stream)
        {
            var reader = new BinaryDataReader(stream);

            stream.Read(Utils.AsSpan(ref BinHeader));
            reader.ReadBytes(64); //padding

            if (BinHeader.NameOffset != 0)
                Name = reader.LoadString(BinHeader.NameOffset - 2);

            //GRSC header
            reader.BaseStream.Read(Utils.AsSpan(ref Header));

            Variations = reader.ReadArray<ShaderVariation>(Header.VariationStartOffset, (int)Header.NumVariation);
        }

        public class ShaderVariation : IResData
        {
            public BnshShaderProgram BinaryProgram { get; set; }

            internal long Position;

            private VariationHeader header;

            public void Read(BinaryDataReader reader)
            {
                Position = reader.BaseStream.Position;

                reader.BaseStream.Read(Utils.AsSpan(ref header));
                var pos = reader.BaseStream.Position;

                BinaryProgram = reader.Read<BnshShaderProgram>(header.BinaryOffset);

                reader.SeekBegin(pos);
            }
        }

        public class BnshShaderProgram : IResData
        {
            public ShaderCode VertexShader { get; set; }
            public ShaderCode FragmentShader { get; set; }
            public ShaderCode GeometryShader { get; set; }
            public ShaderCode ComputeShader { get; set; }
            public ShaderCode HullShader { get; set; }
            public ShaderCode DomainShader { get; set; }

            public ShaderReflectionData VertexShaderReflection { get; set; }
            public ShaderReflectionData FragmentShaderReflection { get; set; }
            public ShaderReflectionData GeometryShaderReflection { get; set; }
            public ShaderReflectionData ComputeShaderReflection { get; set; }
            public ShaderReflectionData HullShaderReflection { get; set; }
            public ShaderReflectionData DomainShaderReflection { get; set; }

            public byte[] MemoryData = new byte[256];

            public BnshShaderProgramHeader header;

            public void Read(BinaryDataReader reader)
            {
                reader.BaseStream.Read(Utils.AsSpan(ref header));
                var pos = reader.BaseStream.Position;

                VertexShader = reader.Read<ShaderCode>(header.VertexShaderOffset);
                FragmentShader = reader.Read<ShaderCode>(header.FragmentShaderOffset);
                GeometryShader = reader.Read<ShaderCode>(header.GeometryShaderOffset);
                ComputeShader = reader.Read<ShaderCode>(header.ComputeShaderOffset);
                HullShader = reader.Read<ShaderCode>(header.HullShaderOffset);
                DomainShader = reader.Read<ShaderCode>(header.DomainShaderOffset);

                reader.SeekBegin(header.ObjectOffset);
                MemoryData = reader.ReadBytes((int)header.ObjectSize);

                if (header.ShaderReflectionOffset != 0)
                {
                    reader.SeekBegin(header.ShaderReflectionOffset);
                    //offsets
                    var offsets = reader.ReadUInt64s(6);
                    VertexShaderReflection = reader.Read<ShaderReflectionData>(offsets[0]);
                    HullShaderReflection = reader.Read<ShaderReflectionData>(offsets[1]);
                    DomainShaderReflection = reader.Read<ShaderReflectionData>(offsets[2]);
                    GeometryShaderReflection = reader.Read<ShaderReflectionData>(offsets[3]);
                    FragmentShaderReflection = reader.Read<ShaderReflectionData>(offsets[4]);
                    ComputeShaderReflection = reader.Read<ShaderReflectionData>(offsets[5]);
                }

                reader.SeekBegin(pos);
            }
        }

        public class ShaderCode : IResData
        {
            public byte[] ControlCode;
            public byte[] ByteCode;

            public byte[] Reserved = new byte[32];

            public void Read(BinaryDataReader reader)
            {
                reader.ReadBytes(8); //always empty
                ulong controlCodeOffset = reader.ReadUInt64();
                ulong byteCodeOffset = reader.ReadUInt64();
                uint byteCodeSize = reader.ReadUInt32();
                uint controlCodeSize = reader.ReadUInt32();
                Reserved = reader.ReadBytes(32); //padding

                ControlCode = reader.ReadCustom(() =>
                {
                    return reader.ReadBytes((int)controlCodeSize);
                }, controlCodeOffset);

                ByteCode = reader.ReadCustom(() =>
                {
                    return reader.ReadBytes((int)byteCodeSize);
                }, byteCodeOffset);
            }
        }

        public class ShaderReflectionData : IResData
        {
            public BnshShaderReflectionHeader header;

            public ResDict<ResString> Inputs = new ResDict<ResString>();
            public ResDict<ResString> Outputs = new ResDict<ResString>();
            public ResDict<ResString> Samplers = new ResDict<ResString>();
            public ResDict<ResString> ConstantBuffers = new ResDict<ResString>();
            public ResDict<ResString> UnorderedAccessBuffers = new ResDict<ResString>();

            public int[] Slots = new int[0];

            public void Read(BinaryDataReader reader)
            {
                reader.BaseStream.Read(Utils.AsSpan(ref header));
                var pos = reader.BaseStream.Position;

                Inputs = reader.LoadDictionary<ResString>(header.InputDictionaryOffset);
                Outputs = reader.LoadDictionary<ResString>(header.OutputDictionaryOffset);
                Samplers = reader.LoadDictionary<ResString>(header.SamplerDictionaryOffset);
                ConstantBuffers = reader.LoadDictionary<ResString>(header.ConstantBufferDictionaryOffset);
                UnorderedAccessBuffers = reader.LoadDictionary<ResString>(header.UnorderedAccessBufferDictionaryOffset);

                if (header.SlotCount > 0)
                    Slots = reader.ReadCustom(() => reader.ReadInt32s((int)header.SlotCount), (uint)header.SlotOffset);

                reader.SeekBegin(pos);
            }

            public int GetInputLocation(string key)
            {
                 var index = this.Inputs.Keys.ToList().IndexOf(key);
                if (Slots.Length > index)
                    return Slots[index];
                return -1;
            }

            public int GetConstantBufferLocation(string key)
            {
                var index = this.Inputs.Keys.ToList().IndexOf(key);
                if (Slots.Length > index + this.header.ConstBufferIdx)
                    return Slots[index + this.header.ConstBufferIdx];
                return -1;
            }

            public int GetSamplerLocation(string key)
            {
                var index = this.Samplers.Keys.ToList().IndexOf(key);
                if (Slots.Length > index + this.header.SamplerIdx)
                    return Slots[index + this.header.SamplerIdx];
                return -1;
            }

            public int GetOutputLocation(string key)
            {
                var index = this.Outputs.Keys.ToList().IndexOf(key);
                if (Slots.Length > index + this.header.OutputIdx)
                    return Slots[index + this.header.OutputIdx];
                return -1;
            }
        }
    }
}
