using EffectLibraryTest;
using ShaderLibrary.CompileTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.Test
{
    public class TestBNSH
    {
        public static void Run()
        {
            Run("LEye.bnsh_fsh", "output.bnsh");
        }

        public static void Run(string path, string output)
        {
            BnshFile bnsh = new BnshFile(path);

            //get target variation data
            var program = bnsh.Variations[0].BinaryProgram;
            //edit vertex/fragment stages
            var vert = program.VertexShader;
            var frag = program.FragmentShader;

            //useful info

            void PrintReflection(BnshFile.ShaderReflectionData reflection)
            {
                foreach (var name in reflection.Inputs.Keys)
                    Console.WriteLine($"Attribute {name} {reflection.GetInputLocation(name)}");
                foreach (var name in reflection.Outputs.Keys)
                    Console.WriteLine($"Outputs {name} {reflection.GetOutputLocation(name)}");

                foreach (var name in reflection.ConstantBuffers.Keys)
                    Console.WriteLine($"Block {name} {reflection.GetConstantBufferLocation(name)}");

                foreach (var name in reflection.Samplers.Keys)
                    Console.WriteLine($"Sampler {name} {reflection.GetSamplerLocation(name)}");
            }

            if (program.VertexShaderReflection != null)
                PrintReflection(program.VertexShaderReflection);

            if (program.FragmentShaderReflection != null)
                PrintReflection(program.FragmentShaderReflection);


            //extract test
            ShaderExtract.Export(vert, "Vertex.vert");
            ShaderExtract.Export(frag, "Pixel.frag");

            //replace and compile test
            UAMShaderCompiler.Compile(vert, "Vertex.vert", "vert");
            UAMShaderCompiler.Compile(frag, "Pixel.frag", "frag");

            //save 
            bnsh.Save(output);
        }
    }
}
