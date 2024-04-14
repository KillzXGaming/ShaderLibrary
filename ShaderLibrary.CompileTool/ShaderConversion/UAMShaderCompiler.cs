using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShaderLibrary.BnshFile;

namespace ShaderLibrary.CompileTool
{
    public class UAMShaderCompiler
    {
        public static ShaderOutput CompileByText(BnshFile.ShaderCode binary, string text, string kind)
        {
            File.WriteAllText("input.glsl", text);
            return Compile(binary, "input.glsl", kind);
        }

        public static ShaderOutput CompileByText(BnshFile.ShaderCode binary, string text, string kind, Dictionary<string, string> macros)
        {
            File.WriteAllText("input.glsl", CompileMacros(macros, text));
            return Compile(binary, "input.glsl", kind);
        }

        public static ShaderOutput Compile(BnshFile.ShaderCode binary, string shadername, string kind)
        {
            if (binary == null)
                return null;

            //load the original control shader
            var control = new ControlShader(binary.ControlCode);
            //Get the original constants
            var constants = control.GetConstants(binary.ByteCode);

            bool isSucess = ExecuteCommand($"uam.exe {shadername} -o out.raw -s {kind}");
            if (!isSucess)
            {
                Console.WriteLine($"Failed to compile {shadername}! Will fallback to original shader.");
                //use original shaders
                return new ShaderOutput()
                {
                    ShaderCode = binary.ByteCode,
                    Control = binary.ControlCode,
                };
            }

            byte[] shader = FixHeader(File.ReadAllBytes("out.raw"));

            control.SetConstants(shader, constants, out byte[] shader_updated);

            var mem = new MemoryStream();
            control.Save(mem);

            Console.WriteLine($"{binary.ByteCode.Length} {shader_updated.Length}");

            binary.ByteCode = shader_updated.ToArray();
            binary.ControlCode = mem.ToArray();

            return new ShaderOutput()
            {
                ShaderCode = shader_updated,
                Control = mem.ToArray(),
            };
        }

        static byte[] FixHeader(byte[] data)
        {
            var mem = new MemoryStream();
            using (var reader = new BinaryReader(new MemoryStream(data)))
            using (var writer = new BinaryWriter(mem))
            {
                //DKSH header skip
                reader.BaseStream.Seek(304, SeekOrigin.Begin);
                var byte_code = reader.ReadBytes((int)reader.BaseStream.Length - 304);

                //nvn header
                writer.Write(305419896);
                writer.Write(new byte[44]);

                //raw byte code
                writer.Write(byte_code);
            }
            return mem.ToArray();
        }

        static void AlignBytes(BinaryWriter wr, int align, byte pad_val = 0)
        {
            var startPos = wr.BaseStream.Position;
            long position = wr.Seek((int)(-wr.BaseStream.Position % align + align) % align, SeekOrigin.Current);

            wr.Seek((int)startPos, System.IO.SeekOrigin.Begin);
            while (wr.BaseStream.Position != position)
            {
                wr.Write((byte)pad_val);
            }
        }

        static bool ExecuteCommand(string Command)
        {
            ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/K " + Command);
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Normal;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            Process cmd = new Process();
            cmd.StartInfo = info;
            cmd.OutputDataReceived += (sender, e) =>
            {

            };
            cmd.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($"Error: {e.Data}");
            };
            cmd.Start();

            cmd.BeginOutputReadLine();
            cmd.BeginErrorReadLine();

            cmd.WaitForExit();

            return cmd.ExitCode == 0;
        }

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
                            value = string.Format("#define {0} {1}", macroName, macros[macroName]);
                            Console.WriteLine($"macro {value}");
                        }
                    }
                    writer.WriteLine(value);
                }
            }
            return sb.ToString();
        }

        public class ShaderOutput
        {
            public byte[] ShaderCode;
            public byte[] Control;
        }
    }
}
