using BfshaLibrary.WiiU;
using Microsoft.VisualBasic.FileIO;
using ShaderLibrary.Common;
using ShaderLibrary.Helpers;
using ShaderLibrary.IO;
using ShaderLibrary.WiiU;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        //Wii U specific
        public ushort Flags;
        public uint DataAlignment;

        public bool IsWiiU = false;

        public StringPool StringPool = new StringPool();

        public BfshaFile() {
            BinHeader = new BinaryHeader();
            BinHeader.Magic = 2314885531374736198;
        }

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
            if (IsWiiU)
            {
                BfshaSaverWiiU bfshaSaverWiiU = new BfshaSaverWiiU();
                using (var writer = new BinaryDataWriter(stream, true))
                    bfshaSaverWiiU.Save(this, writer);
            }
            else
            {
                BfshaSaver saver = new BfshaSaver();
                using (var writer = new BinaryDataWriter(stream))
                    saver.Save(this, writer);
            }
        }

        private bool IsBfshaWiiU(Stream stream)
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
            if (IsBfshaWiiU(stream))
            {
                BfshaLoaderWiiU.Load(this, new BinaryDataReader(stream, true));
                return;
            }
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

        public void ExportProgram(Stream stream, string name, ShaderModel shader, params BfshaShaderProgram[] programs)
        {
            var programIndices = programs.Select(x => shader.Programs.IndexOf(x)).ToList();
            ExportProgram(stream, name, shader, programIndices.ToArray());
        }

        public void ExportProgram(Stream stream, string name, ShaderModel shader, params int[] programIndices)
        {
            var bfsha = CreateNewArchive(name, shader, programIndices);
            bfsha.Save(stream);
        }

        public BfshaFile CreateNewArchive(string name, ShaderModel shader, params int[] programIndices)
        {
            //Export as a separate usable bfsha binary
            var programs = programIndices.Select(x => shader.Programs[x]).ToList();

            BfshaFile bfsha = new BfshaFile()
            {
                BinHeader = BinHeader,
                Name = name,
                Path = this.Path,
            };
            List<int> search_keys = new List<int>();

            //go through the key table and get the option keys used for the specific programs
            var num_keys = shader.StaticKeyLength + shader.DynamicKeyLength;
            for (int i = 0; i < programIndices.Length; i++)
            {
                var key_idx = num_keys * programIndices[i];
                for (int j = 0; j < num_keys; j++)
                    search_keys.Add(shader.KeyTable[key_idx + j]);
            }

            BnshFile bnsh = new BnshFile()
            {
                BinHeader = shader.BnshFile.BinHeader,
                Name = shader.BnshFile.Name,
                Header = shader.BnshFile.Header,
                Variations = new List<ShaderVariation>(),
            };

            List<BfshaShaderProgram> exported_programs = new List<BfshaShaderProgram>();

            foreach (var prog in programs)
            {
                //Setup program
                exported_programs.Add(new BfshaShaderProgram()
                {
                    UsedAttributeFlags = prog.UsedAttributeFlags,
                    Flags = prog.Flags,
                    ImageIndices = prog.ImageIndices,
                    SamplerIndices = prog.SamplerIndices,
                    UniformBlockIndices = prog.UniformBlockIndices,
                    StorageBufferIndices = prog.StorageBufferIndices,
                    VariationIndex = bnsh.Variations.Count,
                });
                //add variation from the bnsh to newly made one
                bnsh.Variations.Add(shader.BnshFile.Variations[prog.VariationIndex]);
            }

            ShaderModel export_shader = new ShaderModel()
            {
                DynamicKeyLength = shader.DynamicKeyLength,
                StaticKeyLength = shader.StaticKeyLength,
                Samplers = shader.Samplers,
                Attributes = shader.Attributes,
                SymbolData = shader.SymbolData,
                StaticOptions = shader.StaticOptions,
                DynamicOptions = shader.DynamicOptions,
                StorageBuffers = shader.StorageBuffers,
                UniformBlocks = shader.UniformBlocks,
                Images = shader.Images,
                DefaultProgramIndex = -1,
                KeyTable = search_keys.ToArray(),
                Name = name,
                Unknown2 = shader.Unknown2,
                BlockIndices = shader.BlockIndices,
                UnknownIndices2 = shader.UnknownIndices2,
                BnshFile = bnsh,
                Programs = exported_programs,
            };
            bfsha.ShaderModels.Add(export_shader.Name, export_shader);

            return bfsha;
        }
    }

    public class ShaderModel : IResData
    {
        public string Name { get; set; }

        public ResDict<ShaderOption> StaticOptions { get; set; } = new ResDict<ShaderOption>();
        public ResDict<ShaderOption> DynamicOptions { get; set; } = new ResDict<ShaderOption>();
        public List<BfshaShaderProgram> Programs { get; set; } = new List<BfshaShaderProgram>();

        public ResDict<BfshaStorageBuffer> StorageBuffers { get; set; } = new ResDict<BfshaStorageBuffer>();
        public ResDict<BfshaUniformBlock> UniformBlocks { get; set; } = new ResDict<BfshaUniformBlock>();

        public ResDict<BfshaImageBuffer> Images { get; set; } = new ResDict<BfshaImageBuffer>();
        public ResDict<BfshaSampler> Samplers { get; set; } = new ResDict<BfshaSampler>();
        public ResDict<BfshaAttribute> Attributes { get; set; } = new ResDict<BfshaAttribute>();

        public SymbolData SymbolData { get; set; } 

        public BnshFile BnshFile { get; set; }

        public int DefaultProgramIndex = -1;

        public byte StaticKeyLength;
        public byte DynamicKeyLength;

        public int[] KeyTable { get; set; }

        public byte Unknown2;
        public byte[] BlockIndices = new byte[4]; //material, shape, skeleton, option block indices
        public byte[] UnknownIndices2 = new byte[4];

        public int MaxVSRingItemSize = 0;
        public int MaxRingItemSize = 0;

        public void Read(BinaryDataReader reader)
        {
            Name = reader.LoadString();
            StaticOptions = reader.LoadDictionary<ShaderOption>();
            DynamicOptions = reader.LoadDictionary<ShaderOption>();
            Attributes = reader.LoadDictionary<BfshaAttribute>();
            Samplers = reader.LoadDictionary<BfshaSampler>();
            if (reader.Header.VersionMajor >= 8)
                Images = reader.LoadDictionary<BfshaImageBuffer>();
            UniformBlocks = reader.LoadDictionary<BfshaUniformBlock>();
            long uniformArrayOffset = reader.ReadInt64();

            if (reader.Header.VersionMajor >= 7)
            {
                StorageBuffers = reader.LoadDictionary<BfshaStorageBuffer>();
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
            BlockIndices = reader.ReadBytes(4);

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

            Programs = reader.ReadArray<BfshaShaderProgram>(
                 (ulong)shaderProgramArrayOffset,
                 programCount);

            foreach (var program in this.Programs)
                program.ParentShader = this;

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

        public ShaderVariation GetVariation(BfshaShaderProgram program)
        {
            if (program == null) return null;

            return this.BnshFile.Variations[program.VariationIndex];
        }

        public int GetProgramIndex(Dictionary<string, string> options)
        {
            return ShaderOptionSearcher.GetProgramIndex(this, options);
        }

        public List<int> GetProgramIndexList(Dictionary<string, string> options)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < Programs.Count; i++)
            {
                if (ShaderOptionSearcher.IsValidProgram(this, i, options))
                    indices.Add(i);
            }
            return indices;
        }

        public void CreateAddNewShaderProgram(ShaderVariation variation, Dictionary<string, string> options)
        {
            // Add variation
            if (!this.BnshFile.Variations.Contains(variation))
                this.BnshFile.Variations.Add(variation);

            var program = new BfshaShaderProgram();
            program.VariationIndex = this.BnshFile.Variations.IndexOf(variation);

            //Init locations
            //TODO add arguments with used bindings
            for (int i = 0; i < this.Samplers.Count; i++)
                program.SamplerIndices.Add(new ShaderIndexHeader());

            for (int i = 0; i < this.UniformBlocks.Count; i++)
                program.UniformBlockIndices.Add(new ShaderIndexHeader());

            for (int i = 0; i < this.StorageBuffers.Count; i++)
                program.StorageBufferIndices.Add(new ShaderIndexHeader());

            for (int i = 0; i < this.Attributes.Count; i++)
                program.SetAttribute(i, false);

            for (int i = 0; i < this.Samplers.Count; i++)
            {
            }

            //expand key table
            int[] program_keys = new int[this.StaticKeyLength + this.DynamicKeyLength];
            KeyTable = KeyTable.Concat(program_keys).ToArray();

            // Add program
            var programIndex = this.Programs.Count;
            this.Programs.Add(program);

            //Set option combinations required to use the program
            SetProgramOptions(programIndex, options);
        }

        public void CreateAddNewShaderProgramWiiU(BfshaShaderProgram program, Dictionary<string, string> options)
        {
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
            //The amount of keys used per program
            int numKeysPerProgram = this.StaticKeyLength + this.DynamicKeyLength;

            //Static key (total * program index)
            int baseIndex = numKeysPerProgram * programIdx;
            //current choice
            int choiceIndex = option.Choices.Keys.ToList().IndexOf(choice);
            if (choiceIndex == -1)
                throw new Exception($"Invalid choice input ({choice}) for {option.Name}!");

            int key_idx = baseIndex + option.Bit32Index;

            option.SetKey(ref this.KeyTable[key_idx], choiceIndex);

            var new_choiceIdx = option.GetChoiceIndex(this.KeyTable[key_idx]);
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

        public void DumpProgramChoices(int programIndex, string path)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine($"--------------------------------------------------------");

                int numKeysPerProgram = StaticKeyLength + DynamicKeyLength;

                var maxBit = this.StaticOptions.Values.Max(x => x.Bit32Index);
                int baseIndex = numKeysPerProgram * programIndex;
                for (int j = 0; j < this.StaticOptions.Count; j++)
                {
                    var option = this.StaticOptions[j];
                    int choiceIndex = option.GetChoiceIndex(KeyTable[baseIndex + option.Bit32Index]);
                    if (choiceIndex > option.Choices.Count || choiceIndex == -1)
                        throw new Exception($"Invalid choice index in key table! {option.Name} index {choiceIndex}");

                    writer.WriteLine($"{option.Name} = {option.Choices.GetKey(choiceIndex)}");
                }

                for (int j = 0; j < this.DynamicOptions.Count; j++)
                {
                    var option = this.DynamicOptions[j];
                    int ind = option.Bit32Index - option.KeyOffset;
                    int choiceIndex = option.GetChoiceIndex(KeyTable[baseIndex + StaticKeyLength + ind]);
                    if (choiceIndex > option.Choices.Count || choiceIndex == -1)
                        throw new Exception($"Invalid choice index in key table! {option.Name} index {choiceIndex}");

                    writer.WriteLine($"{option.Name} = {option.Choices.GetKey(choiceIndex)}");
                }
            }
        }

        public string GetOptionChoice(int programIndex, string option_name)
        {
            int numKeysPerProgram = StaticKeyLength + DynamicKeyLength;
            int baseIndex = numKeysPerProgram * programIndex;

            if (this.StaticOptions.ContainsKey(option_name))
            {
                var option = this.StaticOptions[option_name];
                int choiceIndex = option.GetChoiceIndex(KeyTable[baseIndex + option.Bit32Index]);
                if (choiceIndex > option.Choices.Count || choiceIndex == -1)
                    throw new Exception($"Invalid choice index in key table! {option.Name} index {choiceIndex}");

                return option.Choices.GetKey(choiceIndex);
            }
            return "";
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
                Samplers = reader.ReadArray<SymbolEntry>(reader.ReadUInt64(), shaderModel.Samplers.Count);
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
            public string Name3 { get; set; }
            public string Value3 { get; set; }

            public void Read(BinaryDataReader reader)
            {
                Name1 = reader.LoadString();
                if (reader.Header.VersionMajor <= 8)
                {
                    Value1 = reader.LoadString();
                    Name2 = reader.LoadString();
                    Value2 = reader.LoadString();
                }
                if (reader.Header.VersionMajor == 8)
                {
                    Name3 = reader.LoadString();
                    Value3 = reader.LoadString();
                }
                if (reader.Header.VersionMajor == 9)
                {
                    Value1 = reader.LoadString();
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

        public ResDict<ResUint32> Choices { get; set; } = new ResDict<ResUint32>();

        public ushort BlockOffset;
        public ushort Padding;
        public short DefaultChoiceIdx;
        public byte KeyOffset;

        public uint Bit32Mask; //bit mask
        public byte Bit32Index; //key index
        public byte Bit32Shift; //key bit pos
        public ushort Padding2;

        public ushort Flags; //wii u only

        public uint[] ChoiceValues = new uint[0];

        internal long _choiceDictOfsPos;
        internal long _choiceValuesOfsPos;

        public string DefaultChoice => Choices.GetKey(DefaultChoiceIdx);

        public void Read(BinaryDataReader reader)
        {
            Name = reader.LoadString();
            Choices = reader.LoadDictionary<ResUint32>(reader.ReadUInt64());
            var choiceValuesOffset = reader.ReadUInt64();
            ushort choiceCount = reader.ReadUInt16();
            if (reader.Header.VersionMajor >= 9)
            {
                DefaultChoiceIdx = reader.ReadInt16();
                Padding = reader.ReadUInt16();
                BlockOffset = reader.ReadByte();
                KeyOffset = reader.ReadByte();
                Bit32Mask = reader.ReadUInt32();
                Bit32Index = reader.ReadByte();
                Bit32Shift = reader.ReadByte();
                uint padding2 = reader.ReadUInt16();
            }
            else
            {
                DefaultChoiceIdx = reader.ReadByte();
                BlockOffset = reader.ReadUInt16(); // Uniform block offset.
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
            // Clear the existing choice index
            int clearedKey = (int)(key & ~this.Bit32Mask);
            // Set the new choice index
            key = clearedKey | (choiceIdx << this.Bit32Shift);
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

        public ResUint32() { }
        public ResUint32(uint value) { this.Value = value; }

        public void Read(BinaryDataReader reader)
        {
            Value = reader.ReadUInt32();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class BfshaUniformBlock : IResData
    {
        public ushort Size => header.Size;
        public byte Index => header.Index;
        public BlockType Type => (BlockType)header.Type;

        public ResDict<BfshaUniform> Uniforms { get; set; } = new ResDict<BfshaUniform>();

        public ShaderUniformBlockHeader header = new ShaderUniformBlockHeader();

        public byte[] DefaultBuffer;

        internal long _uniformVarDictOfsPos;
        internal long _uniformVarOfsPos;
        internal long _defaultDataOfsPos;

        public void Read(BinaryDataReader reader)
        {
            reader.BaseStream.Read(Utils.AsSpan(ref header));

            long pos = reader.BaseStream.Position;

            Uniforms = reader.LoadDictionary<BfshaUniform>(
                header.UniformDictionaryOffset, header.UniformArrayOffset);

            DefaultBuffer = reader.ReadCustom(() => reader.ReadBytes(Size), (uint)header.DefaultOffset);

            reader.SeekBegin(pos);
        }

        public enum BlockType
        {
            None,
            Material,
            Shape,
            Option,
            Num,
        }
    }

    public class BfshaUniform : IResData
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public ushort DataOffset { get; set; }
        public byte BlockIndex { get; set; }

        //Wii U only
        public ushort GX2Count { get; set; }
        public byte GX2Type { get; set; }
        public byte GX2ParamType { get; set; }
        //

        public void Read(BinaryDataReader reader)
        {
            Name = reader.LoadString();
            Index = reader.ReadInt32();
            DataOffset = reader.ReadUInt16();
            BlockIndex = reader.ReadByte();
            reader.ReadByte(); //padding
        }
    }

    public class BfshaImageBuffer : IResData
    {
        public void Read(BinaryDataReader reader)
        {
        }
    }

    public class BfshaStorageBuffer : IResData
    {
        public uint[] Unknowns { get; set; }

        public void Read(BinaryDataReader reader)
        {
            Unknowns = reader.ReadUInt32s(8);
        }
    }

    public class BfshaSampler : IResData
    {
        public string Annotation = "";

        public byte Index = 0;

        //Wii U only
        public byte GX2Type { get; set; } = 0;
        public byte GX2Count { get; set; } = 0;
        //

        public void Read(BinaryDataReader reader)
        {
            Annotation = reader.LoadString();
            Index = reader.ReadByte();
            reader.ReadBytes(7); //padding
        }
    }

    public class BfshaAttribute : IResData
    {
        public byte Index;
        public sbyte Location;

        //Wii U only
        public byte GX2Type { get; set; }
        public byte GX2Count { get; set; }
        //

        public void Read(BinaryDataReader reader)
        {
            Index = reader.ReadByte();
            Location = reader.ReadSByte();
        }
    }

    public class BfshaShaderProgram : IResData
    {
        public List<ShaderIndexHeader> UniformBlockIndices = new List<ShaderIndexHeader>();
        public List<ShaderIndexHeader> SamplerIndices = new List<ShaderIndexHeader>();
        public List<ShaderIndexHeader> ImageIndices = new List<ShaderIndexHeader>();
        public List<ShaderIndexHeader> StorageBufferIndices = new List<ShaderIndexHeader>();

        public int VariationIndex;

        internal ulong VariationOffset;

        public uint UsedAttributeFlags;
        public uint Flags = 0x14;

        internal long _samplerTableOfsPos;
        internal long _uniformBlockTableOfsPos;
        internal long _shaderVariationOfsPos;
        internal long _imageTableOfsPos;
        internal long _storageBlockTableOfsPos;

        public ShaderModel ParentShader { get; internal set; }

        //Wii U only
        public BfshaGX2PixelHeader GX2PixelData;
        public BfshaGX2VertexHeader GX2VertexData;
        public BfshaGX2GeometryHeader GX2GeometryData;

        public ushort[] GX2Instructions = new ushort[10];

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

        public void ResetLocations(ShaderModel shadermodel)
        {
            UniformBlockIndices = new ShaderIndexHeader[shadermodel.UniformBlocks.Count].ToList();
            SamplerIndices   = new ShaderIndexHeader[shadermodel.Samplers.Count].ToList();
            StorageBufferIndices = new ShaderIndexHeader[shadermodel.StorageBuffers.Count].ToList();
            ImageIndices = new ShaderIndexHeader[shadermodel.Images.Count].ToList();

            for (int i = 0; i < UniformBlockIndices.Count; i++)
            {
                UniformBlockIndices[i] = new ShaderIndexHeader();
                UniformBlockIndices[i].VertexLocation = -1;
                UniformBlockIndices[i].FragmentLocation = -1;
                UniformBlockIndices[i].GeoemetryLocation = -1;
                UniformBlockIndices[i].ComputeLocation = -1;
            }
            for (int i = 0; i < SamplerIndices.Count; i++)
            {
                SamplerIndices[i] = new ShaderIndexHeader();
                SamplerIndices[i].VertexLocation = -1;
                SamplerIndices[i].FragmentLocation = -1;
                SamplerIndices[i].GeoemetryLocation = -1;
                SamplerIndices[i].ComputeLocation = -1;
            }
            for (int i = 0; i < StorageBufferIndices.Count; i++)
            {
                StorageBufferIndices[i] = new ShaderIndexHeader();
                StorageBufferIndices[i].VertexLocation = -1;
                StorageBufferIndices[i].FragmentLocation = -1;
                StorageBufferIndices[i].GeoemetryLocation = -1;
                StorageBufferIndices[i].ComputeLocation = -1;
            }
            for (int i = 0; i < ImageIndices.Count; i++)
            {
                ImageIndices[i] = new ShaderIndexHeader();
                ImageIndices[i].VertexLocation = -1;
                ImageIndices[i].FragmentLocation = -1;
                ImageIndices[i].GeoemetryLocation = -1;
                ImageIndices[i].ComputeLocation = -1;
            }
        }

        public void SetSampler(string sampler, bool bind, bool isFragment = true)
        {
            var index = ParentShader.Samplers.GetIndex(sampler);
            if (index == -1)
                throw new Exception($"Sampler not found in shader!");

            var bind_id = ParentShader.Samplers[sampler].Index;

            if (isFragment)
                this.SamplerIndices[index].FragmentLocation = bind ? bind_id : -1;
            else
                this.SamplerIndices[index].VertexLocation = bind ? bind_id : -1;
        }

        public void SetUniformBlock(string sampler, bool bind, bool isFragment)
        {
            var index = ParentShader.UniformBlocks.GetIndex(sampler);
            if (index == -1)
                throw new Exception($"Uniform Block not found in shader!");

            var bind_id = ParentShader.UniformBlocks[sampler].Index;

            if (isFragment)
                this.UniformBlockIndices[index].FragmentLocation = bind ? bind_id : -1;
            else
                this.UniformBlockIndices[index].VertexLocation = bind ? bind_id : -1;
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
            else if (reader.Header.VersionMajor >= 5)
            {
                var header = new ShaderProgramHeaderV5();
                reader.BaseStream.Read(Utils.AsSpan(ref header));

                VariationOffset = header.VariationOffset;
                UsedAttributeFlags = header.UsedAttributeFlags;
                Flags = header.Flags;
                UniformBlockIndices = reader.ReadArray<ShaderIndexHeader>(header.UniformIndexTableBlockOffset, header.NumBlocks);
                SamplerIndices = reader.ReadArray<ShaderIndexHeader>(header.SamplerIndexTableOffset, header.NumSamplers);
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

                if (reader.Header.VersionMajor == 8)
                {
                    reader.ReadInt32();
                    reader.ReadInt32();
                }
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
