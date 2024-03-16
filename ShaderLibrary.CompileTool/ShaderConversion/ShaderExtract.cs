using System.Diagnostics;
using System.Linq;
using System.Text;
using ShaderLibrary;
using ShaderLibrary.CompileTool;

namespace EffectLibraryTest
{
    public class ShaderExtract
    {
        public static void Export(BnshFile.ShaderCode shaderCode, string filePath)
        {
            if (shaderCode == null)
                return;

            File.WriteAllText(filePath, GetCode(shaderCode));
        }

        public static void Export(BnshFile.ShaderCode shaderCode, BnshFile.ShaderReflectionData reflect, string filePath)
        {
            if (shaderCode == null)
                return;

            File.WriteAllText(filePath, GetCode(shaderCode, reflect));
        }

        static string GetCode(BnshFile.ShaderCode shaderCode, BnshFile.ShaderReflectionData reflect = null)
        {
            var control_code = new ControlShader(shaderCode.ControlCode);

            string code = TegraShaderTranslator.Decompile(shaderCode.ByteCode);
            float[] constants = control_code.GetConstantsAsFloats(shaderCode.ByteCode);

            //Apply the code to be usable with the UAM compiler
            code = ApplyConstants(code, constants);
            code = FixLocations(code);

            if (reflect != null)
                code = SetReflectionNames(code, reflect);

            return code;
        }

        static string SetReflectionNames(string code, BnshFile.ShaderReflectionData reflect)
        {
            Dictionary<string, string> symbols = new Dictionary<string, string>();
            foreach (var sampler in reflect.Samplers.Keys)
            {
                int location = reflect.GetSamplerLocation(sampler);
                if (location == -1)
                    continue;

                string glsl_string_vertex = "vp_t_tcb_" + ((location * 2) + 8).ToString("X1");
                string glsl_string_pixel  = "fp_t_tcb_" + ((location * 2) + 8).ToString("X1");
                symbols.Add(glsl_string_vertex, sampler);
                symbols.Add(glsl_string_pixel, sampler);
            }

            foreach (var name in reflect.ConstantBuffers.Keys)
            {
                int location = reflect.GetConstantBufferLocation(name);
                if (location == -1)
                    continue;

                symbols.Add($"_fp_c{((location) + 3)}", $"_{name}");
                symbols.Add($"_vp_c{((location) + 3)}", $"_{name}");

                symbols.Add($"fp_c{((location) + 3)}", name);
                symbols.Add($"vp_c{((location) + 3)}", name);
            }

            foreach (var name in reflect.Inputs.Keys)
            {
                int location = reflect.GetInputLocation(name);
                if (location == -1)
                    continue;

                string glsl_string_input = $"in_attr{location}";
                symbols.Add(glsl_string_input, name);
            }

            foreach (var name in reflect.Outputs.Keys)
            {
                int location = reflect.GetOutputLocation(name);
                if (location == -1)
                    continue;

                string glsl_string_output = $"out_attr{location}";
                symbols.Add(glsl_string_output, name);
            }

            string line;

            var sb = new StringBuilder();
            using (StringReader reader = new StringReader(code))
            {
                do
                {
                    line = reader.ReadLine();

                    if (line != null)
                    {
                        //input sampler
                        foreach (var sampler in symbols)
                        {
                            if (line.Contains(sampler.Key))
                                line = line.Replace(sampler.Key, sampler.Value);
                        }
                        sb.AppendLine(line);
                    }

                } while (line != null);
            }


            return sb.ToString();
        }

        static string FixLocations(string code)
        {
            string line;

            int sampler_bind = 0;
            int block_bind = 0;

            int sampler_base_id = 4;
            int block_base_id = 1;

            var sb = new StringBuilder();
            using (StringReader reader = new StringReader(code))
            {
                do
                {
                    line = reader.ReadLine();

                    if (line != null)
                    {
                        //input sampler
                        if (line.Contains("uniform sampler"))
                        {
                            //get the id in hex
                            string id = line.Split("_").LastOrDefault().Replace(";", "");
                            int slot = Convert.ToInt32($"0x{id}", 16) / 2 - sampler_base_id;

                            //swap binding id with slot id
                            line = line.Replace($"binding = {sampler_bind}", $"binding = {slot}");

                            sampler_bind++;
                        }
                        //input block
                        if (line.Contains("std140) uniform") && line.Contains("_c"))
                        {
                            if (line.EndsWith("_fp_c1")) //remove constant buffer as the extractor loads these directly
                            {
                                //skip cbuffer lines
                                reader.ReadLine();
                                reader.ReadLine();
                                reader.ReadLine();
                                continue;
                            }

                            //get the id in hex
                            string id = line.Split("_c").LastOrDefault().Replace(";", "");
                            int slot = Convert.ToInt32(id);

                            if (slot != 1) //constant buffer skip
                            {
                                //swap binding id with slot id
                                line = line.Replace($"binding = {slot + 1}", $"binding = {slot - 2}");
                            }

                            block_bind++;
                        }
                        sb.AppendLine(line);
                    }

                } while (line != null);
            }


            return sb.ToString();
        }

        static string ApplyConstants(string code, float[] constants)
        {
            string blockName = "fp_c1.data";

            Dictionary<string, float> constant_lookup = new Dictionary<string, float>();

            int index = 0;
            for (int i = 0; i < constants.Length;)
            {
                string swizzle = "x";

                //use each 4 swizzle value
                for (int j = 0; j < 4; j++)
                {
                    if (constants.Length <= i)
                        continue;

                    float value = constants[i];

                    //Expected variable name stored in the block
                    string variable_name = $"{blockName}[{index}].{swizzle}";
                    constant_lookup.Add(variable_name, value);

                    swizzle = SwizzleShift(swizzle);

                    //increase to next constant
                    i++;
                }
                //go to next vec4
                index++;
            }

            string line;

            var sb = new StringBuilder();
            using (StringReader reader = new StringReader(code))
            {
                do
                {
                    line = reader.ReadLine();

                    if (line != null)
                    {
                        //swap variable with raw constant value
                        if (line.Contains("fp_c1.data"))
                        {
                            //find variable and replace it
                            foreach (var var in constant_lookup)
                            {
                                if (line.Contains(var.Key))
                                    line = line.Replace(var.Key, var.Value.ToString());
                            }
                        }

                        sb.AppendLine(line);
                    }

                } while (line != null);
            }


            return sb.ToString();
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
