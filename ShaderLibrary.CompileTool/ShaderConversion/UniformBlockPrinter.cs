using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.CompileTool
{
    public class UniformBlockPrinter
    {
        public static void Print(UniformBlock block)
        {
            //compute a usable uniform block in glsl code

            int offset_cur = 0;

            var uniforms = block.Uniforms.OrderBy(x => x.Value.DataOffset).ToList();

            for (int i = 0;  i < uniforms.Count; i++)
            {
                string name = uniforms[i].Key;
                var offset = uniforms[i].Value.DataOffset - 1;
                var size = 4;
                if (i < uniforms.Count - 1)
                    size = uniforms[i + 1].Value.DataOffset - 1 - offset;

                switch (size)
                {
                    case 4: Console.WriteLine($"float {name};"); break;
                    case 8: Console.WriteLine($"vec2 {name};"); break;
                    case 12: Console.WriteLine($"vec3 {name};"); break;
                    case 16: Console.WriteLine($"vec4 {name};"); break;
                    case 20: 
                        Console.WriteLine($"vec4 {name};");
                        Console.WriteLine($"float padding_{offset};");
                        break;
                    case 32: //texsrt
                        Console.WriteLine($"mat2x4 {name};");
                        break;
                    case 64: //texsrt 4x4
                        Console.WriteLine($"mat4 {name};");
                        break;
                    default:
                        throw new Exception(size.ToString());
                }
            }
        }
    }
}
