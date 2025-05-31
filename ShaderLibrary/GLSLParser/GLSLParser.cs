﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShaderLibrary
{
    public class GLSLParser
    {
        public Dictionary<string, IntermediateShader.OptionMacro> StaticOptions { get; set; } = new();
        public Dictionary<string, IntermediateShader.OptionMacro> DynamicOptions { get; set; } = new();
        public Dictionary<string, IntermediateShader.UniformBlock> UniformBlocks { get; set; } = new();
        public Dictionary<string, IntermediateShader.StorageBlock> StorageBlocks { get; set; } = new();
        public Dictionary<string, IntermediateShader.Sampler> Samplers { get; set; } = new();
        public Dictionary<string, IntermediateShader.Attribute> InputAttributes { get; set; } = new();
        public Dictionary<string, IntermediateShader.Attribute> OutputAttributes { get; set; } = new();

        public GLSLParser() { }

        public GLSLParser(string glsl) { ParseGLSL(glsl); }

        public void ParseGLSL(string glslSource)
        {
            ParseUniformBlocks(glslSource);
            ParseAttributes(glslSource);
            ParseSamplers(glslSource);
            ParseOptions(glslSource);
            ParseRenderInfo(glslSource);
            ParseStorageBuffers(glslSource);

        //   File.WriteAllText("", jsocon.SerializeObject(Shader));
        }

        public BnshFile.ShaderReflectionData GetBnshReflection()
        {
            BnshFile.ShaderReflectionData reflect = new BnshFile.ShaderReflectionData();
            foreach (var attr in this.InputAttributes.Values)
                reflect.Inputs.TryAdd(attr.Symbol, new ResUint32((uint)attr.Location));
            foreach (var attr in this.OutputAttributes.Values)
                reflect.Outputs.TryAdd(attr.Symbol, new ResUint32((uint)attr.Location));
            foreach (var attr in this.UniformBlocks.Values)
                reflect.ConstantBuffers.TryAdd(attr.Symbol, new ResUint32((uint)attr.Location));
            foreach (var attr in this.StorageBlocks.Values)
                reflect.UnorderedAccessBuffers.TryAdd(attr.Symbol, new ResUint32((uint)attr.Location));
            foreach (var attr in this.Samplers.Values)
                reflect.Samplers.TryAdd(attr.Symbol, new ResUint32((uint)attr.Location));

            reflect.UpdateSlots();
            return reflect;
        }

        private static readonly Regex AttributeUniformRegex = new Regex(
    @"(?:layout\s*\(.*location\s*=\s*(\d+).*\)\s*)?(in|out|attribute)\s+([\w\d_]+)\s+([\w\d_]+)\s*;",
            RegexOptions.Compiled);

        private static readonly Regex UniformBlockRegex = new Regex(
        @"(layout\s*\(.*\)\s*uniform\s+\w+\s+\w+\s*{\s*(.*?)\s*};)\s*//\s*@\s*(.*)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex PropertyCommentRegex = new Regex(
            @"\/\/@\s*(\w+)\s*=\s*""([^""]+)""",
            RegexOptions.Compiled);

        private static readonly Regex UniformSamplerRegex = new Regex(
    @"(?:layout\s*\(\s*(?:location|binding)\s*=\s*(?<binding>\d+)\s*\)\s*)?uniform\s+(?<samplerType>sampler\w+)\s+(?<samplerName>\w+)\s*;\s*//\s*@\s*(?<properties>.*)",
            RegexOptions.Compiled);

        private static readonly Regex MacrosRegex = new Regex(
        @"#define\s+(?<name>\w+)\s+(?<value>\w+)\s*\/\/@\s*(?<macroProperties>(\w+\s*=\s*""[^""]+""\s*)*)(?:\s*flags\s*=\s*""(?<flags>[^""]+)""\s*)?",
       RegexOptions.Compiled);

        private static readonly Regex StorageBufferRegex = new Regex(
            @"layout\s*\(\s*(?<layout>[^\)]+)\)\s*buffer\s+(?<blockName>\w+)\s*(\/\/@\s*(?<blockProperties>(\w+\s*=\s*""[^""]+""\s*)+))?\s*\{(?<blockContent>(.|\n)*?)\}\s*\w*\s*;",
        RegexOptions.Compiled);

        private static readonly Regex BindingRegex = new Regex(
            @"binding\s*=\s*(\d+)",
        RegexOptions.Compiled);

        public void ParseAttributes(string glslSource)
        {
            foreach (Match match in AttributeUniformRegex.Matches(glslSource))
            {
                string location = match.Groups[1].Value;
                string qualifier = match.Groups[2].Value;
                string type = match.Groups[3].Value;
                string name = match.Groups[4].Value;
                string properties = match.Groups[5].Value;
                string symbol = name;

                if (qualifier != "in" && qualifier != "out")
                    continue;

                int array_count = 1;
                IntermediateShader.ValueType value_type = IntermediateShader.ValueType.FLOAT;
                (value_type, array_count) = GetValueType(type, array_count);

                // Extract properties attached to the declaration
                foreach (Match propertyMatch in PropertyCommentRegex.Matches(match.Value))
                {
                    string propertyName = propertyMatch.Groups[1].Value;
                    string propertyValue = propertyMatch.Groups[2].Value;

                    switch (propertyName)
                    {
                        case "id": name = propertyValue; break;
                    }
                }

                if (qualifier == "in")
                {
                    if (!this.InputAttributes.ContainsKey(name))
                        this.InputAttributes.Add(name, new IntermediateShader.Attribute()
                        {
                            Symbol = symbol,
                            Location = int.Parse(location),
                            Type = value_type,
                            ArrayCount = (uint)array_count,
                        });
                }
                else
                {
                    if (!this.OutputAttributes.ContainsKey(name))
                        this.OutputAttributes.Add(name, new IntermediateShader.Attribute()
                        {
                            Symbol = symbol,
                            Location = int.Parse(location),
                            Type = value_type,
                            ArrayCount = (uint)array_count,
                        });
                }
            }

        }

        public void ParseSamplers(string glslSource)
        {
            // Match uniform samplers
            foreach (Match match in UniformSamplerRegex.Matches(glslSource))
            {
                string samplerType = match.Groups["samplerType"].Value;
                string samplerName = match.Groups["samplerName"].Value; 
                string symbol = samplerName;
                string properties = match.Groups["properties"].Value; 

                int location = -1; 
                if (match.Groups["binding"].Success)
                {
                    location = int.Parse(match.Groups["binding"].Value);
                }

                // Extract properties attached to the sampler
                foreach (Match propertyMatch in PropertyCommentRegex.Matches(match.Value))
                {
                    string propertyName = propertyMatch.Groups[1].Value;
                    string propertyValue = propertyMatch.Groups[2].Value;

                    switch (propertyName)
                    {
                        case "id": samplerName = propertyValue; break;
                    }
                }

                switch (samplerType)
                {
                    case "sampler1D":
                    case "sampler2D":
                    case "sampler3D":
                    case "sampler2DArray":
                    case "samplerCube":
                    case "samplerCubeArray":
                        if (!this.Samplers.ContainsKey(samplerName))
                            this.Samplers.Add(samplerName, new IntermediateShader.Sampler()
                            {
                                Location = location,
                                Symbol = symbol,
                                Type = GetType(samplerType),
                            });
                        break;
                }
            }
        }

        public void ParseOptions(string glslSource)
        {
            foreach (Match macroMatch in MacrosRegex.Matches(glslSource))
            {
                string name = macroMatch.Groups["name"].Value;
                string value = macroMatch.Groups["value"].Value;
                string macroProperties = macroMatch.Groups["macroProperties"].Value;
                string symbol = name;

                //options must be in int form, so ensure booleans are converted
                string GetChoiceValue(string v)
                {
                    if (v == "false") return "0";
                    else if (v == "true") return "1";
                    return v;
                }

                List<string> choices = new List<string>();
                //boolean type
                if (value == "false" || value == "true")
                {
                    choices.Add("0");
                    choices.Add("1");
                }
                else
                    choices.Add(GetChoiceValue(value));

                bool compile_all = false;
                bool is_static = true;
                string desc = "";
                if (!string.IsNullOrEmpty(macroProperties))
                {
                    var macroPropertyMatches = Regex.Matches(macroProperties, @"(?<property>\w+)\s*=\s*""(?<value>[^""]+)""");
                    foreach (Match propertyMatch in macroPropertyMatches)
                    {
                        string property = propertyMatch.Groups["property"].Value;
                        string valueProp = propertyMatch.Groups["value"].Value;

                        switch (property)
                        {
                            case "branch": is_static = valueProp != "dynamic"; break;
                            case "desc": desc = valueProp; break;
                            case "flags": compile_all = valueProp.Contains("compile_all_coices"); break;
                            case "id": name = valueProp; break;
                            case "choices":
                                choices.Clear();
                                foreach (var v in valueProp.Split(" "))
                                    choices.Add(GetChoiceValue(v));

                                //slight hack atm
                                if (!valueProp.Contains(value))
                                {
                                    value = choices.FirstOrDefault();
                                }
                                break;
                        }
                    }
                }

                if (this.StaticOptions.ContainsKey(name) || this.DynamicOptions.ContainsKey(name))
                    continue;

                var option = new IntermediateShader.OptionMacro();
                option.Choices = choices.Distinct().ToList();
                option.DefaultChoice = GetChoiceValue(value);
                option.Symbol = symbol;
                option.CompileAllChoices = compile_all;
                option.Description = desc;

                if (is_static)
                    this.StaticOptions.Add(name, option);
                else
                    this.DynamicOptions.Add(name, option);
            }
        }

        public void ParseRenderInfo(string glslSource)
        {

        }

        public void ParseStorageBuffers(string glslSource)
        {
            foreach (Match blockMatch in StorageBufferRegex.Matches(glslSource))
            {
                string layout = blockMatch.Groups["layout"].Value;
                string blockName = blockMatch.Groups["blockName"].Value;
                string blockProperties = blockMatch.Groups["blockProperties"].Value;
                string blockContent = blockMatch.Groups["blockContent"].Value;
                string type = "0";
                string symbol = blockName;
                uint blockSize = 0;

                // Parse and display block properties
                var blockPropertyMatches = Regex.Matches(blockProperties, @"(?<property>\w+)\s*=\s*""(?<value>[^""]+)""");
                foreach (Match propertyMatch in blockPropertyMatches)
                {
                    string property = propertyMatch.Groups["property"].Value;
                    string value = propertyMatch.Groups["value"].Value;

                    switch (property)
                    {
                        case "id": blockName = value; break;
                        case "type": type = value; break;
                        case "size": uint.TryParse(value, out blockSize); break;
                    }
                }

                if (this.StorageBlocks.ContainsKey(blockName))
                    continue;

                IntermediateShader.StorageBlock block = new IntermediateShader.StorageBlock();
                this.StorageBlocks.Add(blockName, block);

                int binding = 0;
                int.TryParse(layout, out binding);

                block.Location = binding;
                block.Size = blockSize;
                block.Symbol = symbol;
            }
        }

        public void ParseUniformBlocks(string glslSource)
        {
            // Pattern to match uniform blocks
            string blockPattern = @"layout\s*\(\s*(?<layout>[^\)]+)\)\s*uniform\s+(?<blockName>\w+)\s*(\/\/@\s*(?<blockProperties>(\w+\s*=\s*""[^""]+""\s*)+))?\s*\{(?<blockContent>(.|\n)*?)\}\s*\w*\s*;";

            // Pattern to match uniforms inside blocks
            string uniformPattern = @"(?<datatype>\w+)\s+(?<name>\w+)\s*(\[(?<arraySize>[^\]]+)\])?\s*;\s*(\/\/@\s*(?<uniformProperties>(\w+\s*=\s*""[^""]+""\s*)+))?";

            // Match and parse uniform blocks
            var blockMatches = Regex.Matches(glslSource, blockPattern);

            foreach (Match blockMatch in blockMatches)
            {
                string layout = blockMatch.Groups["layout"].Value;
                string blockName = blockMatch.Groups["blockName"].Value;
                string blockProperties = blockMatch.Groups["blockProperties"].Value;
                string blockContent = blockMatch.Groups["blockContent"].Value;
                string type = "0";
                string symbol = blockName;
                uint blockSize = 0;

                // Parse and display block properties
                var blockPropertyMatches = Regex.Matches(blockProperties, @"(?<property>\w+)\s*=\s*""(?<value>[^""]+)""");
                foreach (Match propertyMatch in blockPropertyMatches)
                {
                    string property = propertyMatch.Groups["property"].Value;
                    string value = propertyMatch.Groups["value"].Value;

                    switch (property)
                    {
                        case "id": blockName = value; break;
                        case "type": type = value; break;
                        case "size":  uint.TryParse(value, out blockSize);  break;
                    }
                }

                if (this.UniformBlocks.ContainsKey(blockName))
                    continue;

                IntermediateShader.UniformBlock block = new IntermediateShader.UniformBlock();
                this.UniformBlocks.Add(blockName, block);

                int binding = 0;

                foreach (Match match in BindingRegex.Matches(layout))
                {
                    string bindingNumber = match.Groups[1].Value;
                    int.TryParse(bindingNumber, out binding);
                }

                block.Location = binding;
                block.Size = blockSize;
                block.Symbol = symbol;

                switch (type)
                {
                    case "material": block.Type = IntermediateShader.BlockType.Material; break;
                    case "shape": block.Type = IntermediateShader.BlockType.Shape; break;
                    case "option": block.Type = IntermediateShader.BlockType.Option; break;
                    case "skeleton": block.Type = IntermediateShader.BlockType.Skeleton; break;
                    case "resmaterial": block.Type = IntermediateShader.BlockType.ResMaterial; break;
                }

                if (type == "material" || type == "scene")
                {
                    var mem = new MemoryStream();
                    using (var writer = new BinaryWriter(mem))
                    {
                        // Match and parse uniforms inside the block
                        var uniformMatches = Regex.Matches(blockContent, uniformPattern);
                        foreach (Match uniformMatch in uniformMatches)
                        {
                            string datatype = uniformMatch.Groups["datatype"].Value;
                            string name = uniformMatch.Groups["name"].Value;
                            string arraySize = uniformMatch.Groups["arraySize"].Value;
                            string properties = uniformMatch.Groups["uniformProperties"].Value;

                            int array_count = 1;
                          //  int.TryParse(arraySize, out array_count);

                            IntermediateShader.ValueType value_type = IntermediateShader.ValueType.FLOAT;

                            (value_type, array_count) = GetValueType(datatype, array_count);

                            var offset = writer.BaseStream.Position;

                            if (!string.IsNullOrEmpty(properties))
                            {
                                var propertyMatches = Regex.Matches(properties, @"(?<property>\w+)\s*=\s*""(?<value>[^""]+)""");
                                foreach (Match propertyMatch in propertyMatches)
                                {
                                    string property = propertyMatch.Groups["property"].Value;
                                    string value = propertyMatch.Groups["value"].Value;

                                    switch (property)
                                    {
                                        case "id": name = value; break;
                                        case "default_value":
                                            string[] data_values = value.Split(" ");
                                            for (int i = 0; i < data_values.Length; i++)
                                                writer.Write(float.Parse(data_values[i]));
                                            break;
                                    }
                                }
                            }

                            if (block.Uniforms.ContainsKey(name))
                                continue;

                            block.Uniforms.Add(name, new IntermediateShader.Uniform()
                            {
                                Offset = (uint)offset + 1,
                                Type = value_type,
                                ArrayCount = (uint)array_count
                            });
                        }
                    }
                    block.Buffer = mem.ToArray();
                    if (block.Buffer.Length > 0)
                        block.Size = (uint)block.Buffer.Length;
                }
            }
        }


        private (IntermediateShader.ValueType, int) GetValueType(string type, int count)
        {
            switch (type)
            {
                //represent matrices as vec4[] as done generally by game shaders
                case "mat4": return (IntermediateShader.ValueType.FLOAT4, count * 4);
                case "mat2x4": return (IntermediateShader.ValueType.FLOAT4, count * 2);
                case "mat4x2": return (IntermediateShader.ValueType.FLOAT4, count * 2);
                case "bool": return (IntermediateShader.ValueType.BOOL, count);
                case "float": return (IntermediateShader.ValueType.FLOAT, count);
                case "vec2": return (IntermediateShader.ValueType.FLOAT2, count);
                case "vec3": return (IntermediateShader.ValueType.FLOAT3, count);
                case "vec4": return (IntermediateShader.ValueType.FLOAT4, count);
                case "int": return (IntermediateShader.ValueType.INT, count);
                case "ivec2": return (IntermediateShader.ValueType.INT2, count);
                case "ivec3": return (IntermediateShader.ValueType.INT3, count);
                case "ivec4": return (IntermediateShader.ValueType.INT4, count);
            }
            throw new Exception($"value type not supported {type}!");
        }

        private IntermediateShader.Sampler.SamplerType GetType(string type)
        {
            switch (type)
            {
                case "sampler1D": return IntermediateShader.Sampler.SamplerType.Sampler1D;
                case "sampler2D": return IntermediateShader.Sampler.SamplerType.Sampler2D;
                case "sampler3D": return IntermediateShader.Sampler.SamplerType.Sampler3D;
                case "sampler2DArray": return IntermediateShader.Sampler.SamplerType.Sampler2DArray;
                case "samplerCube": return IntermediateShader.Sampler.SamplerType.SamplerCube;
                case "samplerCubeArray": return IntermediateShader.Sampler.SamplerType.SamplerCubeArray;
            }
            throw new Exception($"sampler type not supported {type}!");
        }
    }
}
