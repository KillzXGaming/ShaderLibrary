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
            Run("shader.bnsh", "output.bnsh");
        }

        public static void Run(string path, string output)
        {
            BnshFile bnsh = new BnshFile(path);

            //get target variation data
            var program = bnsh.Variations[0].BinaryProgram;
            //edit vertex/fragment stages
            var vert = program.VertexShader;
            var frag = program.FragmentShader;

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
