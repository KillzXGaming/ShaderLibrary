using ShaderLibrary.IO;
using ShaderLibrary.Loading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static ShaderLibrary.BnshFile;

namespace ShaderLibrary
{
    public class BfshaFile
    {
        public ResDict<ShaderModel> ShaderModels = new ResDict<ShaderModel>();

        public string Name { get; set; }
        public string Path { get; set; }

        public BinaryHeader BinHeader; //A header shared between bfsha and other formats

        public BfshaFile() { }

        public BfshaFile(string filePath)
        {
            Read(File.OpenRead(filePath));
        }

        public BfshaFile(Stream stream)
        {
            Read(stream);
        }

        public void Save(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                Save(fs);
        }

        public void Save(Stream stream)
        {
            BfshaSaver saver = new BfshaSaver();
            using (var writer = new BinaryDataWriter(stream))
                saver.Save(this, writer);
        }

        private bool IsWiiU(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                reader.ReadUInt32(); //magic
                return reader.ReadUInt32() != 0x20202020;
            }
        }

        public void Read(Stream stream)
        {
            stream.Position = 0;

            var reader = new BinaryDataReader(stream);
            stream.Read(Utils.AsSpan(ref BinHeader));

            reader.Header = BinHeader;

            ulong unk = reader.ReadUInt64();
            ulong stringPoolOffset = reader.ReadUInt64();
            ulong shaderModelOffset = reader.ReadUInt64();
            this.Name = reader.LoadString();
            this.Path = reader.LoadString();
            this.ShaderModels = reader.LoadDictionary<ShaderModel>();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt64();
            if (reader.Header.VersionMajor >= 7)
            {
                if (reader.ReadUInt64() != 0) //padding
                    throw new Exception();
            }

            ushort ModelCount = reader.ReadUInt16();
            ushort flag = reader.ReadUInt16();
            reader.ReadUInt16();
        }
    }

    public class ShaderModel : IResData
    {
        public string Name { get; set; }

        public ResDict<ShaderOption> StaticOptions { get; set; }
        public ResDict<ShaderOption> DynamicOptions { get; set; }
        public List<ShaderProgram> Programs { get; set; }

        public ResDict<StorageBuffer> StorageBuffers { get; set; }
        public ResDict<UniformBlock> UniformBlocks { get; set; }

        public ResDict<ImageBuffer> Images { get; set; }
        public ResDict<Sampler> Samplers { get; set; }
        public ResDict<Attribute> Attributes { get; set; }

        public SymbolData SymbolData { get; set; }

        public BnshFile BnshFile { get; set; }

        public int DefaultProgramIndex = -1;

        public byte StaticKeyLength;
        public byte DynamicKeyLength;

        public int[] KeyTable { get; set; }

        public byte Unknown2;
        public byte[] UnknownIndices = new byte[4];
        public byte[] UnknownIndices2 = new byte[4];

        public void Read(BinaryDataReader reader)
        {
            Name = reader.LoadString();
            StaticOptions = reader.LoadDictionary<ShaderOption>();
            DynamicOptions = reader.LoadDictionary<ShaderOption>();
            Attributes = reader.LoadDictionary<Attribute>();
            Samplers = reader.LoadDictionary<Sampler>();
            if (reader.Header.VersionMajor >= 8)
                Images = reader.LoadDictionary<ImageBuffer>();
            UniformBlocks = reader.LoadDictionary<UniformBlock>();
            long uniformArrayOffset = reader.ReadInt64();

            if (reader.Header.VersionMajor >= 7)
            {
                StorageBuffers = reader.LoadDictionary<StorageBuffer>();
                reader.ReadUInt64(); //unknown offset. Not used in files tested
            }

            long shaderProgramArrayOffset = reader.ReadInt64();
            long keyTableOffset = reader.ReadInt64();
            long shaderArchiveOffset = reader.ReadInt64();
            long symbolInfoOffset = reader.ReadInt64();
            long shaderFileOffset = reader.ReadInt64();
            reader.ReadInt64(); //0
            reader.ReadInt64(); //0
            reader.ReadInt64(); //0

            if (reader.Header.VersionMajor >= 7)
            {
                //padding
                reader.ReadInt64(); //0
                reader.ReadInt64(); //0
            }

            uint uniformCount = reader.ReadUInt32();
            if (reader.Header.VersionMajor >= 7)
                reader.ReadUInt32(); //shaderStorageCount
            DefaultProgramIndex = reader.ReadInt32();
            ushort staticOptionCount = reader.ReadUInt16();
            ushort dynamicOptionCount = reader.ReadUInt16();
            ushort programCount = reader.ReadUInt16();
            if (reader.Header.VersionMajor < 7)
                reader.ReadUInt16(); //unk 

            StaticKeyLength = reader.ReadByte();
            DynamicKeyLength = reader.ReadByte();

            byte attribCount = reader.ReadByte();
            byte samplerCount = reader.ReadByte();

            if (reader.Header.VersionMajor >= 8)
                reader.ReadByte(); //image count

            byte uniformBlockCount = reader.ReadByte();
            Unknown2 = reader.ReadByte();
            UnknownIndices = reader.ReadBytes(4);

            if (reader.Header.VersionMajor >= 8)
                this.UnknownIndices2 = reader.ReadBytes(11);
            else if (reader.Header.VersionMajor >= 7)
                reader.ReadBytes(4);
            else
                reader.ReadBytes(6);

            long pos = reader.Position;

            //Go into the bnsh file and get the file size
            reader.SeekBegin((long)shaderFileOffset + 0x1C);
            var bnshSize = (int)reader.ReadUInt32();

            var bnshFileStream = new SubStream(reader.BaseStream, shaderFileOffset, bnshSize);
            BnshFile = new BnshFile(bnshFileStream);

            Programs = reader.ReadArray<ShaderProgram>(
                 (ulong)shaderProgramArrayOffset,
                 programCount);

            KeyTable = reader.ReadCustom(() =>
            {
                int numKeysPerProgram = this.StaticKeyLength + this.DynamicKeyLength;

                return reader.ReadInt32s(numKeysPerProgram * this.Programs.Count);
            }, (ulong)keyTableOffset);

            if (symbolInfoOffset != 0)
            {
                reader.SeekBegin(symbolInfoOffset);
                SymbolData = new SymbolData();
                SymbolData.Read(reader, this);
            }

            //Compute variation index for saving
            foreach (var program in Programs)
            {
                var variationStartOffset = shaderFileOffset + 192;
                var relative_offset = (long)program.VariationOffset - variationStartOffset;
                program.VariationIndex = (int)(relative_offset / 64);
            }

            reader.SeekBegin(pos);

            Console.WriteLine();
        }

        public ShaderVariation GetVariation(int program_index)
        {
            if (program_index == -1) return null;

            return this.BnshFile.Variations[this.Programs[program_index].VariationIndex];
        }

        public int GetProgramIndex(Dictionary<string, string> options)
        {
            for (int i = 0; i < Programs.Count; i++)
            {
                if (IsValidProgram(i, options))
                    return i;
            }
            return -1;
        }

        public List<int> GetProgramIndexList(Dictionary<string, string> options)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < Programs.Count; i++)
            {
                if (IsValidProgram(i, options))
                    indices.Add(i);
            }
            return indices;
        }

        public bool IsValidProgram(int programIndex, Dictionary<string, string> options)
        {
            //The amount of keys used per program
            int numKeysPerProgram = this.StaticKeyLength + this.DynamicKeyLength;

            //Static key (total * program index)
            int baseIndex = numKeysPerProgram * programIndex;

            for (int j = 0; j < this.StaticOptions.Count; j++)
            {
                var option = this.StaticOptions[j];
                //The options must be the same between bfres and bfsha
                if (!options.ContainsKey(option.Name))
                    continue;

                //Get key in table
                int choiceIndex = option.GetChoiceIndex(KeyTable[baseIndex + option.Bit32Index]);
                if (choiceIndex > option.Choices.Count)
                    throw new Exception($"Invalid choice index in key table! Option {option.Name} choice {options[option.Name]}");

                //If the choice is not in the program, then skip the current program
                var choice = option.Choices.GetKey(choiceIndex);
                if (options[option.Name] != choice)
                    return false;
            }

            for (int j = 0; j < this.DynamicOptions.Count; j++)
            {
                var option = this.DynamicOptions[j];
                if (!options.ContainsKey(option.Name))
                    continue;

                int ind = option.Bit32Index - option.KeyOffset;
                int choiceIndex = option.GetChoiceIndex(KeyTable[baseIndex + this.StaticKeyLength + ind]);
                if (choiceIndex > option.Choices.Count)
                    throw new Exception($"Invalid choice index in key table!");

                var choice = option.Choices.GetKey(choiceIndex);
                if (options[option.Name] != choice)
                    return false;
            }
            return true;
        }

        public void CreateAddNewShaderProgram(ShaderVariation variation, Dictionary<string, string> options)
        {
            var program = new ShaderProgram();
            program.VariationIndex = this.BnshFile.Variations.IndexOf(variation);

            //Init locations
            //TODO add arguments with used bindings

            for (int i = 0; i < this.Samplers.Count; i++)
                program.SamplerIndices.Add(new ShaderIndexHeader());

            for (int i = 0; i < this.UniformBlocks.Count; i++)
                program.SamplerIndices.Add(new ShaderIndexHeader());

            for (int i = 0; i < this.StorageBuffers.Count; i++)
                program.SamplerIndices.Add(new ShaderIndexHeader());

            for (int i = 0; i < this.Attributes.Count; i++)
                program.SetAttribute(i, false);

            //expand key table
            int[] program_keys = new int[this.StaticKeyLength + this.DynamicKeyLength];
            KeyTable = KeyTable.Concat(program_keys).ToArray();

            var programIndex = this.Programs.Count;
            this.Programs.Add(program);

            //Set option combinations required to use the program
            SetProgramOptions(programIndex, options);
        }

        public void SetProgramOptions(int programIndex, Dictionary<string, string> options)
        {
            foreach (var staticOption in StaticOptions.Values)
            {
                string choice = staticOption.DefaultChoice;

                if (options.ContainsKey(staticOption.Name))
                    choice = options[staticOption.Name];

                SetOptionKey(staticOption, choice, programIndex); 
            }
            foreach (var dynamicOption in DynamicOptions.Values)
            {
                string choice = dynamicOption.DefaultChoice;

                if (options.ContainsKey(dynamicOption.Name))
                    choice = options[dynamicOption.Name];

                SetOptionKey(dynamicOption, choice, programIndex);
            }
        }

        public void SetOptionKey(ShaderOption option, string choice, int programIdx)
        {
            //in theory the static options should not share any of the dynamic options
            //for the sake of convience, check by shader option name to tell if it is dynamic
            bool is_dynamic = this.DynamicOptions.ContainsKey(option.Name);

            //The amount of keys used per program
            int numKeysPerProgram = this.StaticKeyLength + this.DynamicKeyLength;

            //Static key (total * program index)
            int baseIndex = numKeysPerProgram * programIdx;
            //current choice
            int choiceIndex = option.Choices.Keys.ToList().IndexOf(choice);
            if (choiceIndex == -1)
                throw new Exception($"Invalid choice input ({choice}) for {option.Name}!");

            int key_idx = baseIndex + option.Bit32Index - (is_dynamic ? option.KeyOffset : 0);

            option.SetKey(ref this.KeyTable[key_idx], choiceIndex);

            var new_choiceIdx = option.GetChoiceIndex(this.KeyTable[baseIndex + option.Bit32Index]);
            if (new_choiceIdx != choiceIndex)
                throw new Exception("Failed to set choice index!");
        }

        /// <summary>
        /// Prints specificed program keys from the given index.
        /// </summary>
        /// <param name="programIndex"></param>
        public void PrintProgramKeys(int programIndex)
        {
            Console.WriteLine($"--------------------------------------------------------");

            int numKeysPerProgram = StaticKeyLength + DynamicKeyLength;

            var maxBit = this.StaticOptions.Values.Max(x => x.Bit32Index);
            int baseIndex = numKeysPerProgram * programIndex;
            for (int j = 0; j < this.StaticOptions.Count; j++)
            {
                var option = this.StaticOptions[j];
                int choiceIndex = option.GetChoiceIndex(KeyTable[baseIndex + option.Bit32Index]);
                if (choiceIndex > option.Choices.Count || choiceIndex == -1)
                    throw new Exception($"Invalid choice index in key table! {option.Name} index {choiceIndex}");

                Console.WriteLine($"{option.Name} = {option.Choices.GetKey(choiceIndex)}");
            }

            for (int j = 0; j < this.DynamicOptions.Count; j++)
            {
                var option = this.DynamicOptions[j];
                int ind = option.Bit32Index - option.KeyOffset;
                int choiceIndex = option.GetChoiceIndex(KeyTable[baseIndex + StaticKeyLength + ind]);
                if (choiceIndex > option.Choices.Count || choiceIndex == -1)
                    throw new Exception($"Invalid choice index in key table! {option.Name} index {choiceIndex}");

                Console.WriteLine($"{option.Name} = {option.Choices.GetKey(choiceIndex)}");
            }
        }
    }

    public class SymbolData
    {
        public IList<SymbolEntry> Samplers = new List<SymbolEntry>();
        public IList<SymbolEntry> Images = new List<SymbolEntry>();
        public IList<SymbolEntry> UniformBlocks = new List<SymbolEntry>();
        public IList<SymbolEntry> StorageBuffers = new List<SymbolEntry>();

        public void Read(BinaryDataReader reader, ShaderModel shaderModel)
        {
            if (reader.Header.VersionMajor >= 8)
            {
                Samplers = reader.ReadArray< SymbolEntry>(reader.ReadUInt64(), shaderModel.Samplers.Count);
                Images = reader.ReadArray<SymbolEntry>(reader.ReadUInt64(), shaderModel.Images.Count);
                UniformBlocks = reader.ReadArray<SymbolEntry>(reader.ReadUInt64(), shaderModel.UniformBlocks.Count);
                StorageBuffers = reader.ReadArray<SymbolEntry>(reader.ReadUInt64(), shaderModel.StorageBuffers.Count);
            }
            else if (reader.Header.VersionMajor >= 7)
            {
                Samplers = reader.ReadArray<SymbolEntry>(reader.ReadUInt64(), shaderModel.Samplers.Count);
                UniformBlocks = reader.ReadArray<SymbolEntry>(reader.ReadUInt64(), shaderModel.UniformBlocks.Count);
                StorageBuffers = reader.ReadArray<SymbolEntry>(reader.ReadUInt64(), shaderModel.StorageBuffers.Count);
                reader.ReadUInt64();
            }
            else
            {
                Samplers = reader.ReadArray<SymbolEntry>(reader.ReadUInt64(), shaderModel.Samplers.Count);
                UniformBlocks = reader.ReadArray<SymbolEntry>(reader.ReadUInt64(), shaderModel.UniformBlocks.Count);
                reader.ReadUInt64();
                reader.ReadUInt64();
            }
        }

        public class SymbolEntry : IResData
        {
            public string Name1 { get; set; }
            public string Value1 { get; set; }
            public string Name2 { get; set; }
            public string Value2 { get; set; }

            public void Read(BinaryDataReader reader)
            {
                Name1 = reader.LoadString();
                if (reader.Header.VersionMajor < 8)
                {
                    Value1 = reader.LoadString();
                    Name2 = reader.LoadString();
                    Value2 = reader.LoadString();
                }
            }

            public override string ToString()
            {
                return Name1.ToString();
            }
        }
    }

    public class ShaderOption : IResData
    {
        public string Name { get; set; }

        public ResDict<ResUint32> Choices { get; set; }

        public ushort BlockOffset;
        public ushort Padding;
        public byte Flag;
        public byte KeyOffset;

        public uint Bit32Mask;
        public byte Bit32Index;
        public byte Bit32Shift;
        public ushort Padding2;

        public uint[] ChoiceValues = new uint[0];

        internal long _choiceDictOfsPos;
        internal long _choiceValuesOfsPos;

        public string DefaultChoice => Choices.Keys.FirstOrDefault();

        public void Read(BinaryDataReader reader)
        {
            Name = reader.LoadString();
            Choices = reader.LoadDictionary<ResUint32>(reader.ReadUInt64());
            var choiceValuesOffset = reader.ReadUInt64();
            ushort choiceCount = reader.ReadUInt16();
            if (reader.Header.VersionMajor >= 9)
            {
                BlockOffset = reader.ReadUInt16(); // Uniform block offset.
                Padding = reader.ReadUInt16();
                Flag = reader.ReadByte();
                KeyOffset = reader.ReadByte();
                Bit32Mask = reader.ReadUInt32();
                Bit32Index = reader.ReadByte();
                Bit32Shift = reader.ReadByte();
                uint padding2 = reader.ReadUInt16();
            }
            else
            {
                BlockOffset = reader.ReadUInt16(); // Uniform block offset.
                Flag = reader.ReadByte();
                KeyOffset = reader.ReadByte();
                Bit32Index = reader.ReadByte();
                Bit32Shift = reader.ReadByte();
                Bit32Mask = reader.ReadUInt32();
                uint padding = reader.ReadUInt32();
            }

            ChoiceValues = reader.ReadCustom(() =>
            {
                return reader.ReadUInt32s(choiceCount);
            }, choiceValuesOffset);
        }


        public int GetChoiceIndex(int key)
        {
            //Find choice index with mask and shift
            return (int)((key & this.Bit32Mask) >> this.Bit32Shift);
        }

        public void SetKey(ref int key, int choiceIdx)
        {
            key = (int)((choiceIdx << this.Bit32Shift) & this.Bit32Mask);
            //verify
            var new_choiceIdx = GetChoiceIndex(key);
            if (new_choiceIdx != choiceIdx)
                throw new Exception();
        }

        public int GetStaticKey()
        {
            var key = this.Bit32Index;
            return (int)((key & this.Bit32Mask) >> this.Bit32Shift);
        }

        public int GetDynamicKey()
        {
            var key = this.Bit32Index - this.KeyOffset;
            return (int)((key & this.Bit32Mask) >> this.Bit32Shift);
        }
    }

    public class ResUint32 : IResData
    {
        public uint Value { get; set; }

        public void Read(BinaryDataReader reader)
        {
            Value = reader.ReadUInt32();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class UniformBlock : IResData
    {
        public ushort Size => header.Size;
        public byte Index => header.Index;
        public byte Type => header.Type;

        public ResDict<ShaderUniform> Uniforms { get; set; }

        private ShaderUniformBlockHeader header;

        public byte[] DefaultBuffer;

        internal long _uniformVarDictOfsPos;
        internal long _uniformVarOfsPos;
        internal long _defaultDataOfsPos;

        public void Read(BinaryDataReader reader)
        {
            reader.BaseStream.Read(Utils.AsSpan(ref header));

            long pos = reader.BaseStream.Position;

            Uniforms = reader.LoadDictionary<ShaderUniform>(
                header.UniformDictionaryOffset, header.UniformArrayOffset);

            DefaultBuffer = reader.ReadCustom(() => reader.ReadBytes(Size), (uint)header.DefaultOffset);

            reader.SeekBegin(pos);
        }
    }

    public class ShaderUniform : IResData
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public ushort DataOffset { get; set; }
        public byte BlockIndex { get; set; }

        public void Read(BinaryDataReader reader)
        {
            Name = reader.LoadString();
            Index = reader.ReadInt32();
            DataOffset = reader.ReadUInt16();
            BlockIndex = reader.ReadByte();
            reader.ReadByte(); //padding
        }
    }

    public class ImageBuffer : IResData
    {
        public void Read(BinaryDataReader reader)
        {
        }
    }

    public class StorageBuffer : IResData
    {
        public uint[] Unknowns { get; set; }

        public void Read(BinaryDataReader reader)
        {
            Unknowns = reader.ReadUInt32s(8);
        }
    }

    public class Sampler : IResData
    {
        public string Annotation;

        public byte Index;

        public void Read(BinaryDataReader reader)
        {
            Annotation = reader.LoadString();
            Index = reader.ReadByte();
            reader.ReadBytes(7); //padding
        }
    }

    public class Attribute : IResData
    {
        public byte Index;
        public sbyte Location;

        public void Read(BinaryDataReader reader)
        {
            Index = reader.ReadByte();
            Location = reader.ReadSByte();
        }
    }

    public class ShaderProgram : IResData
    {
        public List<ShaderIndexHeader> UniformBlockIndices = new List<ShaderIndexHeader>();
        public List<ShaderIndexHeader> SamplerIndices = new List<ShaderIndexHeader>();
        public List<ShaderIndexHeader> ImageIndices = new List<ShaderIndexHeader>();
        public List<ShaderIndexHeader> StorageBufferIndices = new List<ShaderIndexHeader>();

        public int VariationIndex;

        internal ulong VariationOffset;

        public uint UsedAttributeFlags;
        public uint Flags;

        internal long _samplerTableOfsPos;
        internal long _uniformBlockTableOfsPos;
        internal long _shaderVariationOfsPos;
        internal long _imageTableOfsPos;
        internal long _storageBlockTableOfsPos;

        public bool IsAttributeUsed(int index)
        {
            return (UsedAttributeFlags >> index & 0x1) != 0;
        }

        public void SetAttribute(int index, bool bind)
        {
            if (bind)
                UsedAttributeFlags |= (1u << index);
            else
                UsedAttributeFlags &= ~(1u << index);
        }

        public void Read(BinaryDataReader reader)
        {
            if (reader.Header.VersionMajor >= 8)
            {
                var header = new ShaderProgramHeaderV8();
                reader.BaseStream.Read(Utils.AsSpan(ref header));

                VariationOffset = header.VariationOffset;
                UsedAttributeFlags = header.UsedAttributeFlags;
                Flags = header.Flags;

                UniformBlockIndices = reader.ReadArray<ShaderIndexHeader>(header.UniformIndexTableBlockOffset, header.NumBlocks);
                SamplerIndices = reader.ReadArray<ShaderIndexHeader>(header.SamplerIndexTableOffset, header.NumSamplers);
                ImageIndices = reader.ReadArray<ShaderIndexHeader>(header.ImageIndexTableOffset, header.NumImages);
                StorageBufferIndices = reader.ReadArray<ShaderIndexHeader>(header.StorageBufferIndexTableOffset, header.NumStorageBuffers);
            }
            else if (reader.Header.VersionMajor >= 7)
            {
                var header = new ShaderProgramHeaderV7();
                reader.BaseStream.Read(Utils.AsSpan(ref header));

                VariationOffset = header.VariationOffset;
                UsedAttributeFlags = header.UsedAttributeFlags;
                Flags = header.Flags;

                UniformBlockIndices = reader.ReadArray<ShaderIndexHeader>(header.UniformIndexTableBlockOffset, header.NumBlocks);
                SamplerIndices = reader.ReadArray<ShaderIndexHeader>(header.SamplerIndexTableOffset, header.NumSamplers);
                StorageBufferIndices = reader.ReadArray<ShaderIndexHeader>(header.StorageBufferIndexTableOffset, header.NumStorageBuffers);
            }
            else
            {
                var header = new ShaderProgramHeaderV4();
                reader.BaseStream.Read(Utils.AsSpan(ref header));

                VariationOffset = header.VariationOffset;
                UsedAttributeFlags = header.UsedAttributeFlags;
                Flags = header.Flags;

                UniformBlockIndices = reader.ReadArray<ShaderIndexHeader>(header.UniformIndexTableBlockOffset, header.NumBlocks);
                SamplerIndices = reader.ReadArray<ShaderIndexHeader>(header.SamplerIndexTableOffset, header.NumSamplers);
            }
        }
    }

    public class ShaderIndexHeader : IResData
    {
        public int VertexLocation = -1;
        public int GeoemetryLocation = -1;
        public int FragmentLocation = -1;
        public int ComputeLocation = -1;

        public void Read(BinaryDataReader reader)
        {
            if (reader.Header.VersionMajor >= 9)
            {
                VertexLocation = reader.ReadInt32();
                FragmentLocation = reader.ReadInt32();
            }
            else
            {
                VertexLocation = reader.ReadInt32();
                GeoemetryLocation = reader.ReadInt32();
                FragmentLocation = (sbyte)reader.ReadInt32();
                ComputeLocation = (sbyte)reader.ReadInt32();
            }
        }

        public void Write(BinaryDataWriter writer)
        {
            if (writer.Header.VersionMajor >= 9)
            {
                writer.Write(VertexLocation);
                writer.Write(FragmentLocation);
            }
            else
            {
                writer.Write(VertexLocation);
                writer.Write(GeoemetryLocation);
                writer.Write(FragmentLocation);
                writer.Write(ComputeLocation);
            }
        }
    }
}
