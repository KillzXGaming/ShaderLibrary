using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShaderLibrary.CompileTool
{
    public class ShaderLabelUtil
    {
        public static Dictionary<uint, string> HDRTranslateLabels = new Dictionary<uint, string>()
        {
            { 0,  "cPower" },
            { 4,  "cRange" },
        };

        public static Dictionary<uint, string> MdlViewLabels = new Dictionary<uint, string>()
        {
            { 0,  "cView[0]" },
            { 16, "cView[1]" },
            { 32, "cView[2]" },
            { 48, "cViewInv[0]" },
            { 64, "cViewInv[1]" },
            { 80, "cViewInv[2]" },
            { 96,  "cViewProj[0]" },
            { 112, "cViewProj[1]" },
            { 128, "cViewProj[2]" },
            { 144, "cViewProj[3]" },
            { 150, "cViewProjInv[0]" },
            { 176, "cViewProjInv[1]" },
            { 192, "cViewProjInv[2]" },
            { 208, "cProjInv[0]" },
            { 224, "cProjInv[1]" },
            { 240, "cProjInv[2]" },
            { 256, "cProjInv[3]" },
            { 272, "cProjInvNoPos[0]" },
            { 288, "cProjInvNoPos[1]" },
            { 304, "vExposure" },
            { 320, "vDir" },
            { 336, "vZNearFar" },
            { 352, "vTanFov" }, //+ vProjOffset
            { 368, "vScreenSize" },
            { 384, "CameraPosCameraPos" },
        };

        public static Dictionary<uint, string> ContextLabels = new Dictionary<uint, string>()
        {
            { 0,  "cView[0]" },
            { 16, "cView[1]" },
            { 32, "cView[2]" },
            { 48, "cViewProj[0]" },
            { 64, "cViewProj[1]" },
            { 80, "cViewProj[2]" },
            { 96, "cViewProj[3]" },
            { 112, "cProj[0]" },
            { 128, "cProj[1]" },
            { 144, "cProj[2]" },
            { 150, "cProj[3]" },
            { 176, "cViewInv[1]" },
            { 192, "cViewInv[2]" },
            { 208, "cViewInv[0]" },
            { 224, "vZNearFar" },
            { 240, "vScreenSize" },
        };

        public static string PreviewUniforms(string shaderCode, ShaderModel shader, BnshFile.ShaderReflectionData reflect)
        {
            List<UniformBlockInfo> blocks = new List<UniformBlockInfo>();
            List<SamplerInfo> samplers = new List<SamplerInfo>();

            foreach (var block in shader.UniformBlocks.OrderByDescending(x => x.Key))
            {
                UniformBlockInfo info = new UniformBlockInfo();

                string symbol = shader.SymbolData.UniformBlocks[block.Value.Index].Name1;
                if (string.IsNullOrEmpty(symbol))
                    symbol = "SceneMat";

                info.Name = symbol;
                info.Labels = new Dictionary<uint, string>();
                info.Label = symbol;

                var labels = GetUniformLabels(block.Value);
                if (labels?.Count > 0)
                    info.Labels = labels;
                if (symbol == "MdlEnvView")
                    info.Labels = MdlViewLabels;
                if (symbol == "HDRTranslate")
                    info.Labels = HDRTranslateLabels;
                if (symbol == "Context")
                    info.Labels = ContextLabels;

                Console.WriteLine($" {block.Key} {info.Name} labels {labels.Count}");

                blocks.Add(info);
            }

            return PreviewUniforms(shaderCode, samplers, blocks);
        }

        static string PreviewUniforms(string shaderCode, List<SamplerInfo> samplers, List<UniformBlockInfo> blocks)
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                Dictionary<string, string> mappings = new Dictionary<string, string>();

                int index = 0;
                foreach (var block in blocks)
                {
                    var maps = GetUniforms(shaderCode, block.Labels, block.Name);
                    index++;

                    foreach (var map in maps)
                    {
                        if (!mappings.ContainsKey(map.Key))
                            mappings.Add(map.Key, $"{block.Label}.{map.Value}.{map.Key.Last()}");
                    }
                }

                foreach (var sampler in samplers)
                {
                    if (!mappings.ContainsKey(sampler.Name))
                        mappings.Add(sampler.Name, sampler.Label);
                }

                foreach (var map in mappings)
                {
                    Console.WriteLine($"Symbol {map.Key} -> {map.Value}");
                }


                foreach (var line in shaderCode.Split('\n', '\r'))
                {
                    string newline = line;
                    if (string.IsNullOrEmpty(newline))
                        continue;

                    //Uniforms are packed into 16 byte blocks
                    //Check for the uniform block
                    foreach (var val in mappings)
                    {
                        if (line.Contains(val.Key))
                        { 
                            newline = newline.Replace(val.Key, val.Value);

                            Console.WriteLine($"Symbol {val.Key} -> {val.Value}");
                        }
                    }
                    writer.WriteLine(newline);
                }
            }
            return sb.ToString();
        }

        public class UniformBlockInfo
        {
            public Dictionary<uint, string> Labels;
            public string Name;
            public string Label;
        }

        public class SamplerInfo
        {
            public string Label;
            public string Name;
        }

        public static Dictionary<uint, string> GetUniformLabels(BfshaUniformBlock block)
        {
            Dictionary<uint, string> uniforms = new Dictionary<uint, string>();

            foreach (var val in block.Uniforms)
            {
                var offset = (uint)val.Value.DataOffset - 1;
                if (val.Value.DataOffset == 0)
                    offset = (uint)(val.Value.Index * 4);

                if (!uniforms.ContainsKey(offset))
                    uniforms.Add(offset, val.Key);
            }

            return uniforms;
        }

        public static Dictionary<string, string> GetUniforms(string shaderCode, Dictionary<uint, string> labels, string blockName)
        {
            List<string> names = labels.Values.ToList();
            List<uint> offsets = labels.Keys.ToList();

            string swizzle = "x";

            Dictionary<string, string> UniformMapping = new Dictionary<string, string>();
            for (int i = 0; i < labels.Count; i++)
            {
                string name = names[i];
                uint size = 16;
                if (i < offsets.Count - 1)
                    size = offsets[i + 1] - offsets[i];

                //
                int startIndex = ((int)offsets[i]) / 16;
                uint amount = size / 4;

                int index = 0;
                for (int j = 0; j < amount; j++)
                {
                    string key = $"{blockName}._m0[{startIndex + index}].{swizzle}";
                    if (UniformMapping.ContainsKey(key))
                        continue;

                    UniformMapping.Add(key, name);
                    if (swizzle == "w")
                        index++;

                    swizzle = SwizzleShift(swizzle);
                }
            }

            Dictionary<string, string> loadedUniforms = new Dictionary<string, string>();
            foreach (var line in shaderCode.Split('\n', '\r'))
            {
                //Uniforms are packed into 16 byte blocks
                //Check for the uniform block
                foreach (var val in UniformMapping)
                {
                    if (line.Contains(val.Key) && !loadedUniforms.ContainsKey(val.Key))
                        loadedUniforms.Add(val.Key, val.Value);
                }
            }
            return loadedUniforms;
        }

        static string SwizzleShift(string swizzle)
        {
            if (swizzle == "x") return "y";
            if (swizzle == "y") return "z";
            if (swizzle == "z") return "w";
            return "x";
        }
    }
}
