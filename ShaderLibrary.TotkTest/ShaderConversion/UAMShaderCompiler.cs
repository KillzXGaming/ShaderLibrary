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
        public static ShaderOutput Compile(BnshFile.ShaderCode binary, string shadername, string kind)
        {
            if (binary == null)
                return null;

            //load the original control shader
            var control = new ControlShader(binary.ControlCode);
            //Get the original constants
            var constants = control.GetConstants(binary.ByteCode);

            Console.WriteLine($"Compiling {shadername}");

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

            byte[] header = binary.ByteCode.AsSpan().Slice(0, 128).ToArray();

            byte[] shader = FixHeader(header, File.ReadAllBytes("out.raw"));

            control.SetConstants(shader, constants, out byte[] shader_updated);

            var mem = new MemoryStream();
            control.Save(mem);

            Console.WriteLine($"ByteCode created {shader_updated.Length} original {binary.ByteCode.Length}");

            binary.ByteCode = shader_updated.ToArray();
            binary.ControlCode = mem.ToArray();

            return new ShaderOutput()
            {
                ShaderCode = shader_updated,
                Control = mem.ToArray(),
            };
        }

        static byte[] FixHeader(byte[] header, byte[] data)
        {
            var mem = new MemoryStream();
            using (var reader = new BinaryReader(new MemoryStream(data)))
            using (var writer = new BinaryWriter(mem))
            {
                //DKSH header skip
                reader.BaseStream.Seek(304, SeekOrigin.Begin);
                var byte_code = reader.ReadBytes((int)reader.BaseStream.Length - (304));

                //nvn header
               // writer.Write(header);

                writer.Write(305419896);
                writer.Write(new byte[44]);

                //raw byte code
                writer.Write(byte_code);

                writer.Seek(51, SeekOrigin.Begin);
                writer.Write((byte)2);
            }
            return mem.ToArray();
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

        public class ShaderOutput
        {
            public byte[] ShaderCode;
            public byte[] Control;
        }
    }
}
