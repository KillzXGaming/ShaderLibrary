using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.WiiU
{
    public class BfshaGX2ShaderImporter
    {
        public static void Import(ShaderModel shadermodel, BfshaShaderProgram program, GSHFile.GX2Shader shader, Dictionary<string, string> shader_uniforms)
        {
            program.GX2VertexData.Data = shader.VertexData;
            program.GX2VertexData.Regs = shader.VertexHeader.GetRegs();
            program.GX2VertexData.Mode = shader.VertexHeader.Mode;
            program.GX2VertexData.Loops = shader.VertexHeader.Loops;

            program.ResetLocations(shadermodel);

            SetLocations(shadermodel, program, shader.VertexHeader, shader_uniforms);

            program.GX2PixelData.Data = shader.PixelData;
            program.GX2PixelData.Regs = shader.PixelHeader.GetRegs();
            program.GX2PixelData.Mode = shader.PixelHeader.Mode;
            program.GX2PixelData.Loops = shader.PixelHeader.Loops;

            SetLocations(shadermodel, program, shader.PixelHeader, shader_uniforms);
        }

        static void SetLocations(ShaderModel shadermodel, BfshaShaderProgram program,
            GSHFile.GX2VertexHeader shader, Dictionary<string, string> shader_uniforms)
        {
            foreach (var uniformBlock in shader.UniformBlocks)
            {
                if (!shader_uniforms.ContainsKey(uniformBlock.Name))
                {
                    Console.WriteLine($"Failed to bind {uniformBlock.Name}");
                    return;
                }
                string target = shader_uniforms[uniformBlock.Name];
                if (!shadermodel.UniformBlocks.ContainsKey(target))
                    return;

                var bind_info = program.UniformBlockIndices[shadermodel.UniformBlocks[target].Index];
                bind_info.VertexLocation = (int)uniformBlock.Offset;
            }
            foreach (var samplerVar in shader.Samplers)
            {
                if (!shader_uniforms.ContainsKey(samplerVar.Name))
                {
                    Console.WriteLine($"Failed to bind {samplerVar.Name}");
                    return;
                }
                string target = shader_uniforms[samplerVar.Name];
                if (!shadermodel.Samplers.ContainsKey(target))
                    return;

                var bind_info = program.SamplerIndices[shadermodel.Samplers[target].Index];

                bind_info.VertexLocation = (int)samplerVar.Location;
            }

            program.UsedAttributeFlags = 0;
            foreach (var attributeVar in shader.Attributes)
            {
                if (!shader_uniforms.ContainsKey(attributeVar.Name))
                {
                    Console.WriteLine($"Failed to bind {attributeVar.Name}");
                    return;
                }
                string target = shader_uniforms[attributeVar.Name];
                if (shadermodel.Attributes.ContainsKey(target))
                    program.SetAttribute(shadermodel.Attributes[target].Index, true);
            }
        }

        static void SetLocations(ShaderModel shadermodel, BfshaShaderProgram program,
            GSHFile.GX2PixelHeader shader, Dictionary<string, string> shader_uniforms)
        {
            foreach (var uniformBlock in shader.UniformBlocks)
            {
                if (!shader_uniforms.ContainsKey(uniformBlock.Name))
                {
                    Console.WriteLine($"Failed to bind {uniformBlock.Name}");
                    return;
                }
                string target = shader_uniforms[uniformBlock.Name];
                if (!shadermodel.UniformBlocks.ContainsKey(target))
                    return;

                var bind_info = program.UniformBlockIndices[shadermodel.UniformBlocks[target].Index];
                bind_info.FragmentLocation = (int)uniformBlock.Offset;
            }
            foreach (var samplerVar in shader.Samplers)
            {
                if (!shader_uniforms.ContainsKey(samplerVar.Name))
                {
                    Console.WriteLine($"Failed to bind {samplerVar.Name}");
                    return;
                }
                string target = shader_uniforms[samplerVar.Name];
                if (!shadermodel.Samplers.ContainsKey(target))
                    return;

                var bind_info = program.SamplerIndices[shadermodel.Samplers[target].Index];
                bind_info.FragmentLocation = (int)samplerVar.Location;
            }
        }
    }
}