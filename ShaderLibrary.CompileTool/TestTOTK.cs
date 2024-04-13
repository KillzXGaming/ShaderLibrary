using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;
using EffectLibraryTest;
using ShaderLibrary;

namespace ShaderLibrary.CompileTool
{
    public class TestTOTK
    {
        public static void RunDeferredTest()
        {
            RunDeferredTest("SystemModel.DeferredMain.bfres","system.Product.100.product.Nin_NX_NVN.bfsha");

        }

        public static void RunDeferredTest(string bfres_path,  string shader_path)
        {
            var bfsha = new BfshaFile(shader_path);
            ResFile resFile = new ResFile(bfres_path);

            void CompieCustomFieldShader(string mesh_target)
            {
                var program = GetShaderProgram(bfsha, resFile, mesh_target, "gsys_assign_material").Item2;
                UAMShaderCompiler.Compile(program.FragmentShader, "Shader/TOTK/PixelDeferred.frag", "frag");
            }

            CompieCustomFieldShader("field_hybrid");
            CompieCustomFieldShader("field_hybrid_all_shadow");
            CompieCustomFieldShader("field_entrance");
            CompieCustomFieldShader("field_leaf");
            CompieCustomFieldShader("field_miasma");
            CompieCustomFieldShader("field_water");

            bfsha.Save($"{shader_path}.NEW.bfsha");
        }

        public static void Run(string bfres_path, string mesh_name, string shader_path)
        {
            var bfsha = new BfshaFile(shader_path);
            ResFile resFile = new ResFile(bfres_path);

            //main shader
            var program_gbuffer = GetShaderProgram(bfsha, resFile, mesh_name, "gsys_assign_gbuffer");
            //depth for shadow casing
            var program_depth   = GetShaderProgram(bfsha, resFile, mesh_name, "gsys_assign_zonly");
            //forward pass
            var program_mat     = GetShaderProgram(bfsha, resFile, mesh_name, "gsys_assign_material");

            var mesh = resFile.Models[0].Shapes[mesh_name];
            var material = resFile.Models[0].Materials[mesh.MaterialIndex];

            //Custom skin count test
            mesh.VertexSkinCount = 4;
            //Also set in the vertex buffer section
            resFile.Models[0].VertexBuffers[mesh.VertexBufferIndex].VertexSkinCount = 4;

            var option = bfsha.ShaderModels[0].DynamicOptions["gsys_weight"];

            //Set choice program lookup key with new skin count choice
            bfsha.ShaderModels[0].SetOptionKey(option, mesh.VertexSkinCount.ToString(), program_mat.Item1);
            bfsha.ShaderModels[0].SetOptionKey(option, mesh.VertexSkinCount.ToString(), program_gbuffer.Item1);
            bfsha.ShaderModels[0].SetOptionKey(option, mesh.VertexSkinCount.ToString(), program_depth.Item1);

            Dictionary<string, string> macros = new Dictionary<string, string>();
            macros.Add("SKIN_COUNT", mesh.VertexSkinCount.ToString());

            foreach (var op in material.ShaderAssign.ShaderOptions)
                macros.Add(op.Key, op.Value);

            string vertex_shader = File.ReadAllText("Shader/TOTK/Vertex.vert");
            string frag_shader = File.ReadAllText("Shader/TOTK/Pixel.frag");

            UAMShaderCompiler.CompileByText(program_gbuffer.Item2.VertexShader, vertex_shader, "vert", macros);
            UAMShaderCompiler.CompileByText(program_depth.Item2.VertexShader, vertex_shader, "vert", macros);
            UAMShaderCompiler.CompileByText(program_mat.Item2.VertexShader, vertex_shader, "vert", macros);

            UAMShaderCompiler.CompileByText(program_gbuffer.Item2.FragmentShader, frag_shader, "frag", macros);

            bfsha.Save("NEW.bfsha");
            resFile.Save("NEW.bfres");
        }

        static Tuple<int, BnshFile.BnshShaderProgram> GetShaderProgram(BfshaFile bfsha, ResFile resFile, string mesh_name, string pipeline)
        {
            var model = resFile.Models[0];
            var shape = model.Shapes[mesh_name];
            var material = model.Materials[shape.MaterialIndex];
            var shader = bfsha.ShaderModels[material.ShaderAssign.ShadingModelName];

            //get options
            var shader_options = GetOptionSearch(material, shape, pipeline);
            //get program
            var programIdx = shader.GetProgramIndex(shader_options);
            var indices_total = shader.GetProgramIndexList(shader_options);

            Console.WriteLine($"Found index {programIdx} of {indices_total.Count}");

            //get target variation data
            return Tuple.Create(programIdx, shader.GetVariation(programIdx).BinaryProgram);
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

            options.Add("gsys_weight", shape.VertexSkinCount.ToString()); //skin count
            options.Add("gsys_assign_type", pipeline); //material pass

            //render info configures options of compiled shaders (alpha testing and render state)
            var renderMode = material.GetRenderInfoString("gsys_render_state_mode");
            var alphaTest = material.GetRenderInfoString("gsys_alpha_test_enable");

            if (options.ContainsKey("gsys_renderstate"))
                options["gsys_renderstate"] = RenderStateModes[renderMode];

            if (options.ContainsKey("gsys_alpha_test_enable"))
                options["gsys_alpha_test_enable"] = alphaTest == "true" ? "1" : "0";

            return options;
        }

        static Dictionary<string, string> RenderStateModes = new Dictionary<string, string>()
        {
            { "opaque", "0" },
            { "mask", "1" },
            { "translucent", "2" },
            { "custom", "3" },
        };
    }
}
