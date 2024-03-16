using BfresLibrary;
using EffectLibraryTest;
using ShaderLibrary.CompilerTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.CompileTool
{
    public class TestSMO
    {
        public static void Run()
        {
            Run("Mario.bfres", "alRenderMaterial.bfsha");
        }

        public static void Run(string bfresFile, string path)
        {
            //Load shader archive
            BfshaFile bfsha = new BfshaFile(path);

            //Load bfres
            ResFile resFile = new ResFile(bfresFile);

            //get model and find the shader for the given mesh
            var model = resFile.Models[0];

            //export as bfsha test
            ShaderExportTest(bfsha, model, model.Shapes[0]);

            //program edit test
            var program = FindShaderProgram(bfsha, model, model.Shapes[0]);

            //extract the shader
            ShaderExtract.Export(program.VertexShader, program.VertexShaderReflection, "Vertex.vert");
            ShaderExtract.Export(program.FragmentShader, program.FragmentShaderReflection, "Pixel.frag");

            //Recompile test
            UAMShaderCompiler.Compile(program.VertexShader, "Vertex.vert", "vert");
            UAMShaderCompiler.Compile(program.FragmentShader, "Pixel.frag", "frag");

            bfsha.Save("alRenderMaterialRB.bfsha");
        }

        static void ShaderExportTest(BfshaFile bfsha, Model model, Shape shape, string motion_vec = "0")
        {
            Material material = model.Materials[shape.MaterialIndex];
            var shader = bfsha.ShaderModels[material.ShaderAssign.ShadingModelName];

            //All options in relation to the material
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var op in material.ShaderAssign.ShaderOptions)
                options.Add(op.Key, op.Value);

            options["cSkinWeightNum"] = shape.VertexSkinCount.ToString(); //skin count

            //render info configures options of compiled shaders (alpha testing and render state)
            var renderMode = material.GetRenderInfoString("gsys_render_state_mode");
            var alphaTest = material.GetRenderInfoString("gsys_alpha_test_enable");

            if (options.ContainsKey("gsys_renderstate"))
                options["gsys_renderstate"] = RenderStateModes[renderMode];

            if (options.ContainsKey("gsys_alpha_test_enable"))
                options["gsys_alpha_test_enable"] = alphaTest == "true" ? "1" : "0";

            //get all programs related to the material
            var programIdxList = shader.GetProgramIndexList(options);

            //export test
            bfsha.ExportProgram("CustomShader.bfsha", shader, programIdxList.ToArray());
        }

        static BnshFile.BnshShaderProgram FindShaderProgram(BfshaFile bfsha, Model model, Shape shape, string motion_vec = "0")
        {
            Material material = model.Materials[shape.MaterialIndex];

            var shader = bfsha.ShaderModels[material.ShaderAssign.ShadingModelName];

            var programIdx = shader.GetProgramIndex(GetOptionSearch(model, shape, motion_vec));

            return bfsha.ShaderModels[0].GetVariation(programIdx).BinaryProgram;
        }

        static Dictionary<string, string> GetOptionSearch(Model model, Shape shape, string motion_vec)
        {
            var material = model.Materials[shape.MaterialIndex];

            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var op in material.ShaderAssign.ShaderOptions)
                options.Add(op.Key, op.Value);

            options["cSkinWeightNum"] = shape.VertexSkinCount.ToString(); //skin count
            //dynamic options not in bfres
            options.Add("enable_compose_footprint", "0");
            options.Add("enable_compose_capture", "0");
            options.Add("enable_add_stain_proc_texture_3d", "0");
            options.Add("compose_prog_texture0", "0");
            options.Add("enable_parallax_cubemap", "0");
            options.Add("is_output_motion_vec", motion_vec);
            options.Add("material_lod_level", "0");
            options.Add("system_id", "0");

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
