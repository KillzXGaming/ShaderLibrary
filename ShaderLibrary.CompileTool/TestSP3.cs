using BfresLibrary;
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
            Run("Clt_COP001.bfres", "Hoian_UBER.Product.400.product.Nin_NX_NVN.bfsha");
        }

         public static void Run(string res_path, string shader_path)
        {
            BfshaFile bfsha = new BfshaFile(shader_path);
            ResFile resFile = new ResFile(res_path);

            foreach (var model in resFile.Models.Values)
            {
                foreach (var shape in model.Shapes.Values)
                {
                    var material = model.Materials[shape.MaterialIndex];
                    var shader_model = bfsha.ShaderModels[material.ShaderAssign.ShadingModelName];
                    var programIdx = shader_model.GetProgramIndex(GetOptionSearch(material, shape));

                    Console.WriteLine($"{shape.Name} {programIdx}");
            }
            }
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

            options["gsys_alpha_test_func"] = "6";

            if (options.ContainsKey("gsys_renderstate"))
                options["gsys_renderstate"] = RenderStateModes[renderMode];

            if (options.ContainsKey("gsys_alpha_test_enable"))
                options["gsys_alpha_test_enable"] = alphaTest == "true" ? "1" : "0";

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
    }
}
