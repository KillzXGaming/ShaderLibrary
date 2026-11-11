using BfshaLibrary.WiiU;
using Microsoft.VisualBasic.FileIO;
using ShaderLibrary.Common;
using ShaderLibrary.Helpers;
using ShaderLibrary.IO;
using ShaderLibrary.Switch;
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
        public uint Unknown1 = 2877358083;

        public bool IsWiiU = false;

        public StringPool StringPool = new StringPool();

        public BfshaFile() {
            BinHeader = new BinaryHeader();
            BinHeader.Magic = 2314885531374736198;
        }

        public BfshaFile(string filePath) => Read(File.OpenRead(filePath));
        public BfshaFile(Stream stream) => Read(stream);

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
                BfshaLoaderWiiU.Load(this, new BinaryDataReader(stream, true));
            else
                BfshaLoader.Read(stream, this);
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
                IsWiiU = IsWiiU,
                StringPool = StringPool,
                Flags = Flags,
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
                Variations = new List<BnshFile.ShaderVariation>(),
            };
          //  bnsh.BinHeader.VersionMajor = 2;
          //  bnsh.BinHeader.VersionMicro = 5;
           // bnsh.BinHeader.VersionMinor = 1;

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
                    ParentShader = shader, 
                });
                //add variation from the bnsh to newly made one
                bnsh.Variations.Add(new ShaderVariation()
                {
                    BinaryProgram = shader.BnshFile.Variations[prog.VariationIndex].BinaryProgram,
                });
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
                MaxRingItemSize = shader.MaxRingItemSize,
                MaxVSRingItemSize = shader.MaxVSRingItemSize, 
            };
            bfsha.ShaderModels.Add(export_shader.Name, export_shader);

            return bfsha;
        }
    }

    public class ShaderModel : IResData
    {
        /// <summary>
        /// Gets or sets the name of the shader model.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets a lookup of static options.
        /// Used for looking up shader programs.
        /// </summary>
        public ResDict<ShaderOption> StaticOptions { get; set; } = new ResDict<ShaderOption>();
        /// <summary>
        /// Gets or sets a lookup of dynamic options.
        /// Used for looking up shader programs.
        /// </summary>
        public ResDict<ShaderOption> DynamicOptions { get; set; } = new ResDict<ShaderOption>();
        /// <summary>
        /// Gets or sets a list of shader programs.
        /// </summary>
        public List<BfshaShaderProgram> Programs { get; set; } = new List<BfshaShaderProgram>();
        /// <summary>
        /// Gets or sets a lookup of storage buffers.
        /// </summary>
        public ResDict<BfshaStorageBuffer> StorageBuffers { get; set; } = new ResDict<BfshaStorageBuffer>();
        /// <summary>
        /// Gets or sets a lookup of uniform blocks.
        /// </summary>
        public ResDict<BfshaUniformBlock> UniformBlocks { get; set; } = new ResDict<BfshaUniformBlock>();
        /// <summary>
        /// Gets or sets a lookup of image buffers.
        /// </summary>
        public ResDict<BfshaImageBuffer> Images { get; set; } = new ResDict<BfshaImageBuffer>();
        /// <summary>
        /// Gets or sets a lookup of samplers.
        /// </summary>
        public ResDict<BfshaSampler> Samplers { get; set; } = new ResDict<BfshaSampler>();
        /// <summary>
        /// Gets or sets a lookup of attributes.
        /// </summary>
        public ResDict<BfshaAttribute> Attributes { get; set; } = new ResDict<BfshaAttribute>();
        /// <summary>
        /// Gets or sets a table of symbols.
        /// This contains strings which typically match the bnsh reflection names.
        /// </summary>
        public SymbolData SymbolData { get; set; } 
        /// <summary>
        /// Gets or sets the bnsh file used to store Switch shader data.
        /// </summary>
        public BnshFile BnshFile { get; set; }
        /// <summary>
        /// The index for what default program to use when option searching fails.
        /// </summary>
        public int DefaultProgramIndex = -1;
        /// <summary>
        /// The amount of int32 keys used for static options.
        /// </summary>
        public byte StaticKeyLength;
        /// <summary>
        /// The amount of int32 keys used for dynamic options.
        /// </summary>
        public byte DynamicKeyLength;
        /// <summary>
        /// A list of int32 keys used for searching shader programs.
        /// A key is made up of static and dynamic option values set via bits.
        /// Each program has StaticKeyLength + DynamicKeyLength keys in the order of the table.
        /// </summary>
        public int[] KeyTable { get; set; }
        /// <summary>
        /// An unknown value.
        /// </summary>
        public byte Unknown2 = 1;

        /// <summary>
        /// A list of block indices to define material, shape, skeleton, option block indices
        /// </summary>
        public byte[] BlockIndices = new byte[4]; //material, shape, skeleton, option block indices

        /// <summary>
        /// Indices with an unknown purpose.
        /// </summary>
        public byte[] UnknownIndices2 = new byte[4];

        /// <summary>
        /// 
        /// </summary>
        public int MaxVSRingItemSize = 0;

        /// <summary>
        /// 
        /// </summary>
        public int MaxRingItemSize = 0;

        public int Flags = 0;

        public BnshFile.ShaderVariation GetVariation(int program_index)
        {
            if (program_index == -1) return null;

            return this.BnshFile.Variations[this.Programs[program_index].VariationIndex];
        }

        public BnshFile.ShaderVariation GetVariation(BfshaShaderProgram program)
        {
            if (program == null) return null;

            return this.BnshFile.Variations[program.VariationIndex];
        }

        public int GetProgramIndex(Dictionary<string, string> options)
        {
            return ShaderOptionSearcher.GetProgramIndex(this, options);
        }

        public int[] GetKeys(int programIdx)
        {
            int num_keys_per_program = this.StaticKeyLength + this.DynamicKeyLength;
            var idx = num_keys_per_program * programIdx;
            return this.KeyTable.Skip(idx).Take(num_keys_per_program).ToArray();
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

        public class SymbolEntry : IResData
        {
            public string Name1 { get; set; }
            public string Value1 { get; set; }
            public string Name2 { get; set; }
            public string Value2 { get; set; }
            public string Name3 { get; set; }
            public string Value3 { get; set; }

            public override string ToString()
            {
                return Name1.ToString();
            }

            public SymbolEntry() { }
            public SymbolEntry(string name, string symbol)
            {
                Name1 = name;
                Value1 = symbol;
                Name2 = name;
                Value2 = symbol;
                Name3 = name;
                Value3 = symbol;
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
        internal byte Padding;

        //Wii U only
        public ushort GX2Count { get; set; }
        public byte GX2Type { get; set; }
        public byte GX2ParamType { get; set; }
    }

    public class BfshaImageBuffer : IResData
    {

    }

    public class BfshaStorageBuffer : IResData
    {
        public uint[] Unknowns { get; set; }
    }

    public class BfshaSampler : IResData
    {
        public string Annotation = "";

        public byte Index = 0;

        //Wii U only
        public byte GX2Type { get; set; } = 0;
        public byte GX2Count { get; set; } = 0;
    }

    public class BfshaAttribute : IResData
    {
        public byte Index;
        public sbyte Location;

        //Wii U only
        public byte GX2Type { get; set; }
        public byte GX2Count { get; set; }
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
        public BfshaGX2GeometryHeader GX2GeometryCopyData;

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
    }

    public class ShaderIndexHeader : IResData
    {
        public int VertexLocation = -1;
        public int GeoemetryLocation = -1;
        public int FragmentLocation = -1;
        public int ComputeLocation = -1;
    }

    public class GX2ShaderDisplayList
    {
        public byte patchType;
        public byte modeType;
        public ushort padd;
        public uint unk;

        public byte[] Code;
        public byte[] DisplayList;


    }
}
