using ShaderLibrary.Common;
using ShaderLibrary.Helpers;
using ShaderLibrary.WiiU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ShaderLibrary
{
    public class IntermediateShader //for handling conversion between bfsha and glsl
    {
        public Dictionary<string, OptionMacro> StaticOptions { get; set; } = new();
        public Dictionary<string, OptionMacro> DynamicOptions { get; set; } = new();
        public Dictionary<string, UniformBlock> UniformBlocks { get; set; } = new();
        public Dictionary<string, Sampler> Samplers { get; set; } = new();
        public Dictionary<string, Attribute> Attributes { get; set; } = new();

        public string Name { get; set; } = "Dummy";

        public void AddShader(GLSLParser glsl, bool isVertex)
        {
            foreach (var op in glsl.StaticOptions)
                StaticOptions.TryAdd(op.Key, op.Value);
            foreach (var op in glsl.DynamicOptions)
                DynamicOptions.TryAdd(op.Key, op.Value);
            foreach (var block in glsl.UniformBlocks)
                UniformBlocks.TryAdd(block.Key, block.Value);
            foreach (var sampler in glsl.Samplers)
                Samplers.TryAdd(sampler.Key, sampler.Value);

            if (isVertex)
            {
                foreach (var attr in glsl.InputAttributes)
                    Attributes.TryAdd(attr.Key, attr.Value);
            }
        }

        public BfshaFile CreateBfshaFileSwitch()
        {
            BfshaFile bfsha = new BfshaFile();
            bfsha.Name = Name;
            bfsha.Path = "";
            bfsha.IsWiiU = false;
            //Switch
            bfsha.DataAlignment = 4096;
            bfsha.BinHeader.Alignment = 12;
            bfsha.BinHeader.ByteOrder = 65279;
            bfsha.BinHeader.VersionMajor = 3;
            bfsha.BinHeader.VersionMicro = 1;
            bfsha.BinHeader.VersionMinor = 0;
            bfsha.Flags = 0;

            ShaderModel shaderModel = CreateShaderModel();
            bfsha.ShaderModels.Add(shaderModel.Name, shaderModel);

            return bfsha;
        }

        public BfshaFile CreateBfshaFile()
        {
            BfshaFile bfsha = new BfshaFile();
            bfsha.Name = Name;
            bfsha.Path = "";
            bfsha.IsWiiU = true;
            //Wii U
            bfsha.DataAlignment = 256;
            bfsha.BinHeader.ByteOrder = 65279;
            bfsha.BinHeader.VersionMajor = 3;
            bfsha.BinHeader.VersionMicro = 4;
            bfsha.BinHeader.VersionMinor = 2;
            bfsha.Flags = 4;

            ShaderModel shaderModel = CreateShaderModel();
            bfsha.ShaderModels.Add(shaderModel.Name, shaderModel);

            return bfsha;
        }

        public ShaderModel CreateShaderModel()
        {
            ShaderModel shaderModel = new ShaderModel();
            shaderModel.Name = Name;
            shaderModel.BnshFile = new BnshFile();

            ShaderOption ConvertOption(string key, OptionMacro op)
            {
                ShaderOption option = new ShaderOption();
                option.Name = key;

                foreach (var choice in op.Choices)
                    option.Choices.Add(choice, null);

                option.DefaultChoiceIdx = (short)option.Choices.GetIndex(op.DefaultChoice);
                option.BlockOffset = 0;
                option.Flags = 0;
                return option;
            }

            foreach (var op in StaticOptions)
                shaderModel.StaticOptions.Add(op.Key, ConvertOption(op.Key, op.Value));

            foreach (var op in DynamicOptions)
                shaderModel.DynamicOptions.Add(op.Key, ConvertOption(op.Key, op.Value));

            ShaderOptionCreator.SetupOptionKeyFlags(shaderModel);

            foreach (var samp in Samplers)
                shaderModel.Samplers.Add(samp.Key, new BfshaSampler()
                {
                    Index = (byte)shaderModel.Samplers.Count,
                    Annotation = "", 
                    GX2Type = (byte)GetGX2SamplerType(samp.Value.Type),
                    GX2Count = 1,
                });

            foreach (var attr in Attributes)
                shaderModel.Attributes.Add(attr.Key, new BfshaAttribute()
                {
                    Index = (byte)shaderModel.Attributes.Count,
                    Location = (sbyte)attr.Value.Location,
                    GX2Count = (byte)attr.Value.ArrayCount,
                    GX2Type = (byte)GetValueType(attr.Value.Type),
                });

            //unk
            shaderModel.UnknownIndices2[0] = 0;
            shaderModel.UnknownIndices2[1] = 0;

            //for testing
            shaderModel.DefaultProgramIndex = 0;

            foreach (var b in UniformBlocks)
            {
                BfshaUniformBlock block = new BfshaUniformBlock();
                block.header.Size = (ushort)b.Value.Size;
                block.header.Index = (byte)shaderModel.UniformBlocks.Count;
                block.DefaultBuffer = b.Value.Buffer;
                shaderModel.UniformBlocks.Add(b.Key, block);

                //material, shape, skeleton, option block indices
                switch (b.Value.Type)
                {
                    case BlockType.Material: shaderModel.BlockIndices[0] = block.header.Index; break;
                    case BlockType.Shape:    shaderModel.BlockIndices[1] = block.header.Index; break;
                    case BlockType.Skeleton: shaderModel.BlockIndices[2] = block.header.Index; break;
                    case BlockType.Option:   shaderModel.BlockIndices[3] = block.header.Index; break;
                }

                //block types
                switch (b.Value.Type)
                {
                    case BlockType.Material:    block.header.Type = 1; break;
                    case BlockType.Shape:       block.header.Type = 2; break;
                    case BlockType.Skeleton:    block.header.Type = 3; break;
                    case BlockType.Option:      block.header.Type = 4; break;
                    default:                    block.header.Type = 0; break;
                }

                if (block.DefaultBuffer?.Length > 0)
                {
                    foreach (var u in b.Value.Uniforms)
                    {
                        block.Uniforms.Add(u.Key, new BfshaUniform()
                        {
                            BlockIndex = block.Index,
                            DataOffset = (ushort)u.Value.Offset,
                            Index = block.Uniforms.Count,
                            Name = u.Key,
                            GX2Count = (ushort)u.Value.ArrayCount,
                            GX2Type = (byte)GetValueType(u.Value.Type),
                            GX2ParamType = (byte)GetParamType(u.Value.Type, u.Value.ArrayCount)
                        });
                    }
                }

                shaderModel.KeyTable = new int[0];
            }

            return shaderModel;
        }

        static ShaderParamType GetParamType(ValueType type, uint arrayCount)
        {
            switch (type)
            {
                case ValueType.FLOAT: return ShaderParamType.Float;
                case ValueType.FLOAT2: return ShaderParamType.Float2;
                case ValueType.FLOAT3: return ShaderParamType.Float3;
                case ValueType.FLOAT4:
                    if (arrayCount == 2) //TEXSRT
                        return ShaderParamType.TexSrt;

                    return ShaderParamType.Float4;
                case ValueType.INT: return ShaderParamType.Int;
                case ValueType.INT2: return ShaderParamType.Int2;
                case ValueType.INT3: return ShaderParamType.Int3;
                case ValueType.INT4: return ShaderParamType.Int4;
                case ValueType.BOOL: return ShaderParamType.Bool;
            }
            throw new NotImplementedException($"{type} {arrayCount}");
        }

        static GX2ShaderVarType GetValueType(ValueType type)
        {
            switch (type)
            {
                case ValueType.FLOAT: return GX2ShaderVarType.FLOAT;
                case ValueType.FLOAT2: return GX2ShaderVarType.FLOAT2;
                case ValueType.FLOAT3: return GX2ShaderVarType.FLOAT3;
                case ValueType.FLOAT4: return GX2ShaderVarType.FLOAT4;
                case ValueType.INT: return GX2ShaderVarType.INT;
                case ValueType.INT2: return GX2ShaderVarType.INT2;
                case ValueType.INT3: return GX2ShaderVarType.UINT3;
                case ValueType.INT4: return GX2ShaderVarType.INT4;
                case ValueType.BOOL: return GX2ShaderVarType.BOOL;
                case ValueType.MAT2x4: return GX2ShaderVarType.FLOAT2X4;
            }
            throw new NotImplementedException($"{type}");
        }

        static GX2SamplerVarType GetGX2SamplerType(Sampler.SamplerType type)
        {
            switch (type)
            {
                case Sampler.SamplerType.Sampler2D: return GX2SamplerVarType.SAMPLER_2D;
                case Sampler.SamplerType.Sampler3D: return GX2SamplerVarType.SAMPLER_3D;
                case Sampler.SamplerType.SamplerCube: return GX2SamplerVarType.SAMPLER_CUBE;
                case Sampler.SamplerType.SamplerCubeArray: return GX2SamplerVarType.SAMPLER_CUBE_ARRAY;
                case Sampler.SamplerType.Sampler2DArray: return GX2SamplerVarType.SAMPLER_2D_ARRAY;
                case Sampler.SamplerType.Sampler1D: return GX2SamplerVarType.SAMPLER_1D;
            }
            throw new NotImplementedException($"{type}");
        }

        public class OptionMacro
        {
            public string Symbol;
            public List<string> Choices { get; set; } = new List<string>();
            public string DefaultChoice { get; set; }
            public string Description { get; set; }
            public bool CompileAllChoices = false;
        }

        public class StorageBlock
        {
            public string Symbol { get; set; }
            public uint Size { get; set; }

            public int Location { get; set; }
        }

        public class UniformBlock
        {
            public Dictionary<string, Uniform> Uniforms { get; set; } = new Dictionary<string, Uniform>();
            public string Symbol { get; set; }
            public uint Size {  get; set; }

            public BlockType Type { get; set; }

            public byte[] Buffer { get; set; }

            public int Location { get; set; }
        }

        public class Uniform
        {
            public uint Offset { get; set; }
            public ValueType Type { get; set; }
            public uint ArrayCount { get; set; } = 1;
        }

        public class Sampler
        {
            public string Symbol { get; set; }
            public int Location { get; set; }
            public SamplerType Type { get; set; } = SamplerType.Sampler2D;

            public enum SamplerType
            {
                Sampler2D,
                Sampler2DArray,
                SamplerCube,
                SamplerCubeArray,
                Sampler3D,
                Sampler1D,
            }
        }

        public class Attribute
        {
            public string Symbol { get; set; }
            public int Location { get; set; }
            public ValueType Type { get; set; }
            public uint ArrayCount { get; set; } = 1;
        }

        public enum BlockType
        {
            None,
            Option = 1,
            ResMaterial = 2,
            Material = 3,
            Shape = 4,
            Skeleton = 5,
        }

        public enum ValueType
        {
            MAT2x4,
            BOOL,
            FLOAT,
            FLOAT2,
            FLOAT3,
            FLOAT4,
            INT,
            INT2,
            INT3,
            INT4,
        }
    }
}
