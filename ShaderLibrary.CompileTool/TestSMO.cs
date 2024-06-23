using BfresLibrary;
using ShaderLibrary.Test;
using System;
using System.Collections.Generic;
using System.IO;
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
            BfshaFile bfsha = new BfshaFile(File.OpenRead(path));

            //Load bfres
            ResFile resFile = new ResFile(bfresFile);

            //get model and find the shader for the given mesh
            var model = resFile.Models[0];

            GlslcCompilerTool glslc = new GlslcCompilerTool();

            foreach (var shape in model.Shapes.Values)
            {
                Material material = model.Materials[shape.MaterialIndex];
                foreach (var program in FindAllShaderProgram(bfsha, model, shape))
                {

                    Dictionary<string, string> macros = new Dictionary<string, string>();
                    macros.Add("SKIN_COUNT", shape.VertexSkinCount.ToString());
                    macros.Add("enable_add_stain_proc_texture_3d", "false");

                    foreach (var option in material.ShaderAssign.ShaderOptions)
                    {
                        //boolean
                        if (option.Key.StartsWith("enable_") || option.Key.StartsWith("is_"))
                        {
                            string value = option.Value == "1" ? "true" : "false";
                            macros.Add(option.Key, value);
                        }
                        else
                            macros.Add(option.Key, option.Value);
                    }

                    //Add fresnel emission effect test
                    macros["enable_emission"] = "true"; //toggle emission
                    macros["o_emission"] = "60"; //emission using constant color 0
                    macros["sphere_const_color0"] = "2"; //fresnel effect for constant color 0
                    macros["emission_scale_type"] = "7"; //scale by exposure 

                    //here we don't edit the vertex shader for Mario due to precision match issues with depth shader
                    //The issue does not occur on any other materials, just Mario
                  //  UAMShaderCompiler.CompileByText(program.VertexShader, File.ReadAllText($"Shaders/SMO/Vertex.vert"), "vert", macros);
                    UAMShaderCompiler.CompileByText(program.FragmentShader, File.ReadAllText("Shaders/SMO/Pixel.frag"), "frag", macros);
                }
            }

            if (!Directory.Exists("ouput")) Directory.CreateDirectory("ouput");

            bfsha.Save(Path.Combine("ouput", "alRenderMaterial.bfsha"));
        }

        static BnshFile.BnshShaderProgram FindShaderProgram(BfshaFile bfsha, Model model, Shape shape, string dirt_stain_shader = "0")
        {
            Material material = model.Materials[shape.MaterialIndex];

            var shader = bfsha.ShaderModels[material.ShaderAssign.ShadingModelName];

            var programIdx = shader.GetProgramIndex(GetOptionSearch(model, shape, dirt_stain_shader));


            return shader.GetVariation(programIdx).BinaryProgram;
        }

        static List<BnshFile.BnshShaderProgram> FindAllShaderProgram(BfshaFile bfsha, Model model, Shape shape)
        {
            Material material = model.Materials[shape.MaterialIndex];

            var shader = bfsha.ShaderModels[material.ShaderAssign.ShadingModelName];

            Dictionary<string, string> options = GetMaterialOptions(material, shape);

            List<BnshFile.BnshShaderProgram> programs = new List<BnshFile.BnshShaderProgram>();
            foreach (var idx in shader.GetProgramIndexList(options))
                programs.Add(shader.GetVariation(idx).BinaryProgram);

            return programs;
        }

        static Dictionary<string, string> GetOptionSearch(Model model, Shape shape, string dirt_stain_shader )
        {
            var material = model.Materials[shape.MaterialIndex];

            Dictionary<string, string> options = GetMaterialOptions(material, shape);

            //dynamic options not in bfres
            options.Add("enable_compose_footprint", "0");
            options.Add("enable_compose_capture", "0");
            options.Add("enable_add_stain_proc_texture_3d", dirt_stain_shader);
            options.Add("compose_prog_texture0", "0");
            options.Add("enable_parallax_cubemap", "0");
            options.Add("is_output_motion_vec", "0");
            options.Add("material_lod_level", "0");
            options.Add("system_id", "0");

            return options;
        }

        //All option combinations in relation to the material itself
        static Dictionary<string, string> GetMaterialOptions(Material material, Shape shape)
        {
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
