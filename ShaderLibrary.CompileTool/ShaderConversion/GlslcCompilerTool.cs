using ShaderLibrary.CompileTool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShaderLibrary.Test
{
    /// <summary>
    /// Tool for compiling using a game that uses the glslc library with an nso patch
    /// </summary>
    public class GlslcCompilerTool
    {
        class ShaderEntry
        {
            public BnshFile.ShaderCode VertexCode;
            public BnshFile.ShaderCode PixelCode;

            public string VertexCodeSrc;
            public string PixelCodeSrc;

            public bool SetVertexShader = true;
            public bool SetPixelShader = true;
        }

        string default_frag = @"#version 450 core

layout (location = 0) out vec4 out_attr0;

void main()
{
    out_attr0 = vec4(1.0);
    return;
}

";

        List<ShaderEntry> Shaders = new List<ShaderEntry>();

        public void AddShaderEditVertexOnly(BnshFile.ShaderCode vertexCode, string vertexSrc, Dictionary<string, string> macros)
        {
            AddShader(vertexCode, new BnshFile.ShaderCode(), vertexSrc, default_frag, macros, true, false);
        }

        public void AddShaderEditPixelOnly(BnshFile.ShaderCode vertexCode, BnshFile.ShaderCode pixelCode,
            string vertexSrc,  Dictionary<string, string> macros)
        {
            AddShader(vertexCode, pixelCode, vertexSrc, default_frag, macros, false, true);
        }

        public void AddShader(BnshFile.ShaderCode vertexCode, BnshFile.ShaderCode pixelCode,
            string vertexSrc, string pixelSrc, Dictionary<string, string> macros, bool use_vertex = true, bool use_pixel = true)
        {
            Shaders.Add(new ShaderEntry()
            {
                VertexCode = vertexCode,
                PixelCode = pixelCode,
                //Compile with macros and adjusted locations for glslc
                VertexCodeSrc = CompileMacros(macros, FixLocations(vertexSrc)),
                PixelCodeSrc = CompileMacros(macros, FixLocations(pixelSrc)),
                SetVertexShader = use_vertex,
                SetPixelShader = use_pixel,
            });
        }


        public void Run()
        {
            //using headless lib
            if (!Directory.Exists("ryujinx") || Shaders.Count == 0)
                return;

            //Remove previous
            foreach (var file in Directory.GetFiles(Path.Combine("ryujinx", "portable", "sdcard")))
                File.Delete(file);

            //Dump prepare
            for (int i = 0; i < Shaders.Count; i++)
            {
                //Target shader sources
                string frag_code_path = Path.Combine("ryujinx", "portable", "sdcard", $"shader{i}.frag");
                string vert_code_path = Path.Combine("ryujinx", "portable", "sdcard", $"shader{i}.vert");
                File.WriteAllText(vert_code_path, Shaders[i].VertexCodeSrc + "\0", Encoding.ASCII);
                File.WriteAllText(frag_code_path, Shaders[i].PixelCodeSrc + "\0", Encoding.ASCII);
            }

            //run exec and compile code
            string exec = Path.Combine("ryujinx", "Ryujinx.Headless.SDL2.exe");
            string game = Path.Combine("ryujinx", "game.nsp");
            Exec(exec, game);

            //Now edit
            for (int i = 0; i < Shaders.Count; i++)
            {
                var shader = Shaders[i];

                //Get control shader to edit
                ControlShader v_control = shader.SetVertexShader ? new ControlShader(shader.VertexCode.ControlCode) : new ControlShader();
                ControlShader p_control = shader.SetPixelShader  ? new ControlShader(shader.PixelCode.ControlCode): new ControlShader();

                //Target shader binaries
                string frag_code = Path.Combine("ryujinx", "portable", "sdcard", $"fragment{i}.bin.code");
                string frag_control = Path.Combine("ryujinx", "portable", "sdcard", $"fragment{i}.bin.control");
                string vertex_code = Path.Combine("ryujinx", "portable", "sdcard", $"vertex{i}.bin.code");
                string vertex_control = Path.Combine("ryujinx", "portable", "sdcard", $"vertex{i}.bin.control");

                if (shader.SetVertexShader)
                {
                    if (File.Exists(vertex_control))
                        shader.VertexCode.ControlCode = File.ReadAllBytes(vertex_control);
                    if (File.Exists(vertex_code))
                        shader.VertexCode.ByteCode = File.ReadAllBytes(vertex_code);
                }

                if (shader.SetPixelShader)
                {
                    if (File.Exists(frag_control))
                        shader.PixelCode.ControlCode = File.ReadAllBytes(frag_control);
                    if (File.Exists(frag_code))
                        shader.PixelCode.ByteCode = File.ReadAllBytes(frag_code);
                }

                if (shader.SetVertexShader)
                {
                    //Get the generated control code and extract the constants
                    //Put them in the new shader bytecode
                    ControlShader control = new ControlShader(shader.VertexCode.ControlCode);
                    byte[] constants = control.GetConstants(shader.VertexCode.ByteCode);
                    v_control.SetConstants(shader.VertexCode.ByteCode, constants, out byte[] shader_bytecode);

                    var m = new MemoryStream();
                    v_control.Save(m);
                    shader.VertexCode.ControlCode = m.ToArray();
                    shader.VertexCode.ByteCode = shader_bytecode;
                }

                if (shader.SetPixelShader)
                {
                    //Get the generated control code and extract the constants
                    //Put them in the new shader bytecode
                    ControlShader control = new ControlShader(shader.PixelCode.ControlCode);
                    byte[] constants = control.GetConstants(shader.PixelCode.ByteCode);
                    p_control.SetConstants(shader.PixelCode.ByteCode, constants, out byte[] shader_bytecode);

                    var m = new MemoryStream();
                    p_control.Save(m);
                    shader.PixelCode.ControlCode = m.ToArray();
                    shader.PixelCode.ByteCode = shader_bytecode;
                }
            }
        }

        private static void Exec(string exec, string game)
        {
            ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/K " + $"{exec} {game}");
            info.CreateNoWindow = true;
            info.UseShellExecute = true;
            info.CreateNoWindow = true;

            Process cmd = Process.Start(info);
            cmd.WaitForExit();
        }

        public static string CompileMacros(Dictionary<string, string> macros, string src)
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

        static string FixLocations(string code)
        {
            var sb = new System.Text.StringBuilder();
            using (var writer = new System.IO.StringWriter(sb))
            {
                string[] stringSeparators = new string[] { "\r\n" };
                string[] lines = code.Split(stringSeparators, StringSplitOptions.None);

                foreach (var line in lines)
                {
                    string value = line;
                    if (line.Contains("std140) uniform"))
                    {
                        //get the id 
                        Regex regex = new Regex(@"binding\s*=\s*(\d+)");
                        Match match = regex.Match(line);
                        if (match.Success)
                        {
                            int number = int.Parse(match.Groups[1].Value);
                            value = regex.Replace(line, "binding = " + Math.Max((number - 1), 0));
                            Console.WriteLine(value);
                        }
                    }
                    writer.WriteLine(value);
                }
            }
            return sb.ToString();
        }
    }
}
