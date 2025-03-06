using BfresLibrary;
using EffectLibraryTest;
using ShaderLibrary.CompilerTool;
using ShaderLibrary.CompileTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary
{
    public class TestSP3
    {
        public static void Run()
        {
            Run("Hoian_UBER.Nin_NX_NVN.bfsha");
            Run("Clt_COP001.bfres", "Hoian_UBER.Product.920.product.Nin_NX_NVN.bfsha");
        }

        public static void Run(string shader_path)
        {
            BfshaFile bfsha = new BfshaFile(shader_path);
            var program = bfsha.ShaderModels[0].Programs[0];

            var bin = bfsha.ShaderModels[0].GetVariation(program).BinaryProgram;

            ShaderExtract.ExportPreviewed(bfsha.ShaderModels[0], 
                bin.VertexShader, bin.VertexShaderReflection, "sample2.vert");

            ShaderExtract.ExportPreviewed(bfsha.ShaderModels[0],
             bin.FragmentShader, bin.FragmentShaderReflection, "sample2.frag");

            foreach (var attr in bin.VertexShaderReflection.Inputs)
            {
                Console.WriteLine($"layout(location = {attr.Value}) in vec4 {attr.Key}");
            }
        }

        public static void Run(string res_path, string shader_path)
        {
            BfshaFile bfsha = new BfshaFile(shader_path);
            ResFile resFile = new ResFile(res_path);

            foreach (var block in bfsha.ShaderModels[0].UniformBlocks)
                if (block.Value.Type == BfshaUniformBlock.BlockType.Material)
                UniformBlockPrinter.Print(block.Value);

            foreach (var model in resFile.Models.Values)
            {
                foreach (var shape in model.Shapes.Values)
                {
                    var material = model.Materials[shape.MaterialIndex];
                    var shader_model = bfsha.ShaderModels[material.ShaderAssign.ShadingModelName];
                    var options = GetOptionSearch(material, shape);

                    //Add all other valid options
                    foreach (var staticOp in shader_model.StaticOptions)
                    {
                        if (!options.ContainsKey(staticOp.Key))
                            options.Add(staticOp.Key, staticOp.Value.DefaultChoice);
                    }

                    var program = bfsha.ShaderModels[0].GetProgramIndex(options);
                    var var = bfsha.ShaderModels[0].GetVariation(program);

                    var idx = bfsha.ShaderModels[0].Programs[program].VariationIndex;

                    ShaderExtract.ExportPreviewed(bfsha.ShaderModels[0],
                        var.BinaryProgram.VertexShader,
                        var.BinaryProgram.VertexShaderReflection, "test.vert");

                    Dictionary<string, string> macros = new Dictionary<string, string>();
                    foreach (var option in material.ShaderAssign.ShaderOptions)
                        macros.Add(option.Key, option.Value);

                    string vertex = CompileMacros(macros, File.ReadAllText("Shaders\\SP3\\Vertex.vert"));

                    UAMShaderCompiler.CompileByText(var.BinaryProgram.VertexShader, vertex, "vert");

                    ShaderExtract.ExportPreviewed(bfsha.ShaderModels[0],
                        var.BinaryProgram.VertexShader,
                        var.BinaryProgram.VertexShaderReflection, "testRB.vert");

                    //ShaderExtract.Export(var.BinaryProgram.VertexShader,"testRB.vert");

                    

                    return;
                    /*
                                        foreach (var attr in var.BinaryProgram.VertexShaderReflection.Inputs)
                                        {
                                            Console.WriteLine($"{attr.Value}");
                                        }*/

                    //  material.ShaderAssign.ShaderOptions.Clear();
                    ///  foreach (var op in options)
                    //   material.ShaderAssign.ShaderOptions.Add(op.Key, op.Value);
                }
            }

         //   resFile.Save(res_path);
        }

        static Dictionary<string, string> GetOptionSearch(Material material, Shape shape, string pipeline = "gsys_assign_material")
        {
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var op in material.ShaderAssign.ShaderOptions)
            {
                string choice = op.Value.ToString();
                if (op.Value == "True")
                    choice = "1";
                else if (op.Value == "False")
                    choice = "0";
                else if (op.Value == "<Default Value>")
                    continue;

                options.Add(op.Key, choice);
            }

            if (!options.ContainsKey("gsys_weight"))
                options.Add("gsys_weight", shape.VertexSkinCount.ToString()); //skin count
            else
                options["gsys_weight"] = shape.VertexSkinCount.ToString();

            options.Add("gsys_assign_type", pipeline); //material pass

            //render info configures options of compiled shaders (alpha testing and render state)
            var renderMode = material.GetRenderInfoString("gsys_render_state_mode");
            var alphaTest = material.GetRenderInfoString("gsys_alpha_test_enable");

            options["gsys_alpha_test_func"] = "6";

            if (options.ContainsKey("gsys_renderstate"))
                options["gsys_renderstate"] = RenderStateModes[renderMode];

            if (options.ContainsKey("gsys_alpha_test_enable"))
                options["gsys_alpha_test_enable"] = alphaTest == "true" ? "1" : "0";

            string displayFace = "0";

            //display face
        /*    switch (displayFace)
            {
                case "both":
                    display_face = "0"; // both
                    break;
                case "front":
                    display_face = "1"; // front
                    break;
                case "back":
                    display_face = "2"; // back
                    break;
                case "none":
                    display_face = "3"; // none
                    break;
            }*/

            options.Add("gsys_display_face_type", "1");

            return options;
        }

        static Dictionary<string, string> RenderStateModes = new Dictionary<string, string>()
        {
            { "opaque", "0" },
            { "mask", "1" },
            { "translucent", "2" },
            { "custom", "3" },
        };

        static string CompileMacros(Dictionary<string, string> macros, string src)
        {
            var sb = new System.Text.StringBuilder();
            using (var writer = new System.IO.StringWriter(sb))
            {
                string[] stringSeparators = new string[] { "\r\n" };
                string[] lines = src.Split(stringSeparators, StringSplitOptions.None);

                foreach (var line in lines)
                {
                    string value = line;
                    if (line.StartsWith("#define"))
                    {
                        var macroName = line.Split()[1];
                        if (macros.ContainsKey(macroName))
                        {
                            var macroValue = line.Split()[2];
                            bool isBool = macroValue.Contains("true") || macroValue.Contains("false");

                            if (isBool)
                            {
                                if (macros[macroName] == "1") macros[macroName] = "true";
                                if (macros[macroName] == "0") macros[macroName] = "false";
                            }

                            value = string.Format("#define {0} {1}", macroName, macros[macroName]);
                        }
                    }
                    writer.WriteLine(value);
                }
            }
            return sb.ToString();
        }
    }
}
