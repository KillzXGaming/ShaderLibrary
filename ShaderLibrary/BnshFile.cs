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
        public string Name { get; set; } = "dummy";

        public List<ShaderVariation> Variations { get; set; }

        public BinaryHeader BinHeader; //A header shared between bnsh and other formats
        public BnshHeader Header; //Bnsh header

        public int DataAlignment => (1 << BinHeader.Alignment);

        private Stream _stream;

        public BnshFile() {
            Variations = new List<ShaderVariation>();
            BinHeader = new BinaryHeader();
            Header = new BnshHeader();

            BinHeader.Magic = 1213419074;
            BinHeader.VersionMajor = 2;
            BinHeader.VersionMicro = 12;
            BinHeader.VersionMinor = 1;
            BinHeader.Alignment = 12;
            BinHeader.ByteOrder = 65279;
            BinHeader.BlockOffset = 96;

            Header.Magic = 1668510311;
            Header.VariationStartOffset = 192;
            Header.Version = 4;
            Header.CompilerVersion = 68354;
        }

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
            _stream = stream;
            var reader = new BinaryDataReader(stream, false, true);

            stream.Read(Utils.AsSpan(ref BinHeader));
            reader.ReadBytes(64); //padding

            if (BinHeader.NameOffset != 0)
                Name = reader.LoadString(BinHeader.NameOffset - 2);

            //GRSC header
            reader.BaseStream.Read(Utils.AsSpan(ref Header));

            Variations = reader.ReadArray<ShaderVariation>(Header.VariationStartOffset, (int)Header.NumVariation);
        }

        public void ExportVariation(string filePath, params ShaderVariation[] variation)
        {
            BnshFile bnsh = new BnshFile();
            bnsh.Header = this.Header;
            bnsh.BinHeader = this.BinHeader;
            bnsh.Name = this.Name;
            bnsh.Variations.AddRange(variation);
            bnsh.Save(filePath);
        }

        public class ShaderVariation : IResData
        {
            private BnshShaderProgram _program;
            public BnshShaderProgram BinaryProgram
            {
                get
                {
                    if (_program == null)
                        _program = GetBnshShaderProgram();
                    return _program;
                } set => _program = value;
            }

            internal long Position;

            private VariationHeader header;
            private Stream _stream;

            public ShaderVariation()
            {
                header = new VariationHeader();
            }

            public void Read(BinaryDataReader reader)
            {
                _stream = reader.BaseStream;
                Position = reader.BaseStream.Position;

                reader.BaseStream.Read(Utils.AsSpan(ref header));
                var pos = reader.BaseStream.Position;

              //  BinaryProgram = reader.Read<BnshShaderProgram>(header.BinaryOffset);

                reader.SeekBegin(pos);
            }

            public void Export(string filePath)
            {
                BnshFile bnsh = new BnshFile();
                bnsh.Variations.Add(this);
                bnsh.Save(filePath);
            }

            private BnshShaderProgram GetBnshShaderProgram()
            {
                var reader = new BinaryDataReader(_stream, false, true);
                reader.SeekBegin((int)header.BinaryOffset);

                BnshShaderProgram program = new BnshShaderProgram();
                program.Read(reader);
                return program;
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

            public BnshShaderProgram()
            {
                header = new BnshShaderProgramHeader()
                {
                    Flags = 2, ObjectSize = 256,
                    Reserved8 = 128104,
                };
            }

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

            public ResDict<ResUint32> Inputs = new ResDict<ResUint32>();
            public ResDict<ResUint32> Outputs = new ResDict<ResUint32>();
            public ResDict<ResUint32> Samplers = new ResDict<ResUint32>();
            public ResDict<ResUint32> ConstantBuffers = new ResDict<ResUint32>();
            public ResDict<ResUint32> UnorderedAccessBuffers = new ResDict<ResUint32>();

            public int[] Slots = new int[0];

            public ShaderReflectionData()
            {
                header = new BnshShaderReflectionHeader();
            }

            public void Read(BinaryDataReader reader)
            {
                reader.BaseStream.Read(Utils.AsSpan(ref header));
                var pos = reader.BaseStream.Position;

                Inputs = reader.LoadDictionary<ResUint32>(header.InputDictionaryOffset);
                Outputs = reader.LoadDictionary<ResUint32>(header.OutputDictionaryOffset);
                Samplers = reader.LoadDictionary<ResUint32>(header.SamplerDictionaryOffset);
                ConstantBuffers = reader.LoadDictionary<ResUint32>(header.ConstantBufferDictionaryOffset);
                UnorderedAccessBuffers = reader.LoadDictionary<ResUint32>(header.UnorderedAccessBufferDictionaryOffset);

                if (header.SlotCount > 0)
                {
                    Slots = reader.ReadCustom(() => reader.ReadInt32s((int)header.SlotCount), (uint)header.SlotOffset);
                    AssignSlots(Slots);
                }

                reader.SeekBegin(pos);
            }

            /// <summary>
            ///  Assigns slot data as values in each dictionary
            /// </summary>
            /// <param name="slots"></param>
            private void AssignSlots(int[] slots)
            {
                void Set(ResDict<ResUint32> dict, int idx, int startIdx)
                {
                    if (slots.Length > startIdx + idx)
                        dict[idx].Value = (uint)slots[startIdx + idx];
                }

                for (int i = 0; i < Inputs.Count; i++)
                    Set(Inputs, i, 0);
                for (int i = 0; i < Outputs.Count; i++)
                    Set(Outputs, i, this.header.OutputIdx);
                for (int i = 0; i < Samplers.Count; i++)
                    Set(Samplers, i, this.header.SamplerIdx);
                for (int i = 0; i < ConstantBuffers.Count; i++)
                    Set(ConstantBuffers, i, this.header.ConstBufferIdx);
                for (int i = 0; i < UnorderedAccessBuffers.Count; i++)
                    Set(UnorderedAccessBuffers, i, this.header.UnorderedAccessBufferIdx);

                UpdateSlots();
            }

            /// <summary>
            /// Updates all slot data back from dictionaries
            /// </summary>
            public void UpdateSlots()
            {
                List<int> slots = new List<int>();

                void Set(ResDict<ResUint32> dict, ref int startIdx)
                {
                    startIdx = slots.Count;
                    foreach (var val in dict.Values)
                        slots.Add((int)val.Value);
                }

                int first = 0;
                Set(Inputs, ref first);
                Set(Outputs, ref this.header.OutputIdx);
                Set(Samplers, ref this.header.SamplerIdx);
                Set(ConstantBuffers, ref this.header.ConstBufferIdx);
                Set(UnorderedAccessBuffers, ref this.header.UnorderedAccessBufferIdx);

                this.Slots = slots.ToArray();
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
                var index = this.ConstantBuffers.Keys.ToList().IndexOf(key);
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

            public enum LocationKind
            {
                Input,
                Output,
                Sampler,
                ConstantBuffer,
                UnorderedAccessBuffer,
                Image,
            }
        }
    }
}
